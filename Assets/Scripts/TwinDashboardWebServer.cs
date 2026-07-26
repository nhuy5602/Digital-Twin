using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace ConveyorTwin
{
    /// <summary>
    /// A loopback-only web dashboard for the simulation. It never discovers or controls physical equipment.
    /// Browser commands are queued and applied on Unity's main thread.
    /// </summary>
    public class TwinDashboardWebServer : MonoBehaviour
    {
        [Serializable]
        private class DashboardCommand
        {
            public string type;
            public float conveyorSpeedMps;
            public float pumpFlowLitersPerMinute;
            public float infeedMotorSpeedRpm;
            public float starWheelIndexSpeedRpm;
            public float starWheelDwellSeconds;
            public string preset;
            public bool paused;
            public bool newSeed;
        }

        public FillingFilteringDigitalTwin process;
        [Range(1024, 65535)] public int port = 8088;
        public bool autoStart = true;

        private readonly ConcurrentQueue<string> pendingCommands = new ConcurrentQueue<string>();
        private readonly List<WebSocket> sockets = new List<WebSocket>();
        private readonly object socketLock = new object();
        private HttpListener listener;
        private CancellationTokenSource cancellation;
        private string latestSnapshotJson = "{}";
        private string backgroundError;
        private bool errorLogged;

        public bool ServerRunning { get; private set; }
        public string DashboardUrl => $"http://127.0.0.1:{port}/";

        private void Start()
        {
            if (autoStart)
            {
                StartServer();
            }
        }

        private void Update()
        {
            if (process == null)
            {
                return;
            }

            latestSnapshotJson = JsonUtility.ToJson(process.CreateSnapshot());
            ApplyPendingCommands();

            if (!string.IsNullOrEmpty(backgroundError) && !errorLogged)
            {
                errorLogged = true;
                Debug.LogWarning($"Digital Twin web dashboard stopped: {backgroundError}", this);
            }
        }

        private void OnDestroy()
        {
            StopServer();
        }

        public void OpenDashboard()
        {
            Application.OpenURL(DashboardUrl);
        }

        public void StartServer()
        {
            if (ServerRunning || listener != null)
            {
                return;
            }

            try
            {
                listener = new HttpListener();
                listener.Prefixes.Add(DashboardUrl);
                listener.Start();
                cancellation = new CancellationTokenSource();
                ServerRunning = true;
                Debug.Log($"Digital Twin web dashboard available at {DashboardUrl}", this);
                _ = Task.Run(ServerLoopAsync);
                _ = Task.Run(BroadcastLoopAsync);
            }
            catch (Exception exception)
            {
                backgroundError = exception.Message;
                StopServer();
            }
        }

        public void StopServer()
        {
            ServerRunning = false;
            cancellation?.Cancel();
            cancellation?.Dispose();
            cancellation = null;

            if (listener != null)
            {
                try
                {
                    listener.Stop();
                    listener.Close();
                }
                catch (Exception)
                {
                    // Shutdown races are harmless when Play mode ends.
                }
                finally
                {
                    listener = null;
                }
            }

            lock (socketLock)
            {
                foreach (var socket in sockets)
                {
                    try
                    {
                        socket.Abort();
                        socket.Dispose();
                    }
                    catch (Exception)
                    {
                        // Socket already closed.
                    }
                }

                sockets.Clear();
            }
        }

        private async Task ServerLoopAsync()
        {
            try
            {
                while (listener != null && listener.IsListening && cancellation != null && !cancellation.IsCancellationRequested)
                {
                    var context = await listener.GetContextAsync();
                    _ = Task.Run(() => HandleContextAsync(context));
                }
            }
            catch (Exception exception)
            {
                if (cancellation != null && !cancellation.IsCancellationRequested)
                {
                    backgroundError = exception.Message;
                    ServerRunning = false;
                }
            }
        }

        private async Task HandleContextAsync(HttpListenerContext context)
        {
            try
            {
                if (context.Request.Url != null && context.Request.Url.AbsolutePath == "/ws" && context.Request.IsWebSocketRequest)
                {
                    var webSocketContext = await context.AcceptWebSocketAsync(null);
                    await ReceiveCommandsAsync(webSocketContext.WebSocket);
                    return;
                }

                var path = context.Request.Url == null ? "/" : context.Request.Url.AbsolutePath;
                if (path == "/" || path == "/index.html")
                {
                    await WriteHtmlAsync(context.Response);
                    return;
                }

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
            }
            catch (Exception)
            {
                try
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.Close();
                }
                catch (Exception)
                {
                    // Client disconnected before the response was written.
                }
            }
        }

        private async Task ReceiveCommandsAsync(WebSocket socket)
        {
            lock (socketLock)
            {
                sockets.Add(socket);
            }

            var buffer = new byte[4096];
            try
            {
                while (socket.State == WebSocketState.Open && cancellation != null && !cancellation.IsCancellationRequested)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                        break;
                    }

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        pendingCommands.Enqueue(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    }
                }
            }
            catch (Exception)
            {
                // A browser refresh normally ends here.
            }
            finally
            {
                lock (socketLock)
                {
                    sockets.Remove(socket);
                }

                socket.Dispose();
            }
        }

        private async Task BroadcastLoopAsync()
        {
            try
            {
                while (cancellation != null && !cancellation.IsCancellationRequested)
                {
                    await Task.Delay(200, cancellation.Token);
                    var payload = new ArraySegment<byte>(Encoding.UTF8.GetBytes(latestSnapshotJson ?? "{}"));
                    WebSocket[] activeSockets;
                    lock (socketLock)
                    {
                        activeSockets = sockets.ToArray();
                    }

                    foreach (var socket in activeSockets)
                    {
                        if (socket.State != WebSocketState.Open)
                        {
                            continue;
                        }

                        try
                        {
                            await socket.SendAsync(payload, WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                        catch (Exception)
                        {
                            // ReceiveCommandsAsync removes the closed socket.
                        }
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Expected during shutdown.
            }
        }

        private void ApplyPendingCommands()
        {
            while (pendingCommands.TryDequeue(out var json))
            {
                var command = JsonUtility.FromJson<DashboardCommand>(json);
                if (command == null || string.IsNullOrEmpty(command.type))
                {
                    continue;
                }

                switch (command.type)
                {
                    case "setpoints":
                        process.ApplySetpoints(new TwinSetpoints
                        {
                            conveyorSpeedMps = command.conveyorSpeedMps,
                            pumpFlowLitersPerMinute = command.pumpFlowLitersPerMinute,
                            infeedMotorSpeedRpm = command.infeedMotorSpeedRpm,
                            starWheelIndexSpeedRpm = command.starWheelIndexSpeedRpm,
                            starWheelDwellSeconds = command.starWheelDwellSeconds
                        });
                        break;
                    case "pause":
                        process.SetSimulationPaused(command.paused);
                        break;
                    case "reset":
                        process.ResetSimulation(command.newSeed);
                        break;
                    case "preset":
                        if (Enum.TryParse(command.preset, true, out TwinScenarioPreset preset))
                        {
                            process.ApplyPreset(preset);
                        }
                        break;
                }
            }
        }

        private static async Task WriteHtmlAsync(HttpListenerResponse response)
        {
            var bytes = Encoding.UTF8.GetBytes(DashboardHtml);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentEncoding = Encoding.UTF8;
            response.ContentLength64 = bytes.Length;
            response.Headers.Add("Cache-Control", "no-store");
            await response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
            response.Close();
        }

        private const string DashboardHtml = @"<!doctype html>
<html><head><meta charset=""utf-8""><meta name=""viewport"" content=""width=device-width,initial-scale=1""><title>Digital Twin Dashboard</title>
<style>
*{box-sizing:border-box}body{margin:0;background:#07111e;color:#eaf5ff;font-family:Segoe UI,Arial,sans-serif}main{max-width:1180px;margin:auto;padding:22px}h1{margin:0;color:#68d5ff;font-size:25px}.sub{color:#9db3c7;margin:5px 0 15px}.grid{display:grid;grid-template-columns:320px 1fr;gap:16px}.panel{background:#0d1b2c;border:1px solid #1d3854;border-radius:10px;padding:16px}label{display:block;font-size:13px;color:#b7cce0;margin-top:12px}input{width:100%;accent-color:#39c9ff}output{float:right;color:#fff;font-weight:600}button{background:#176795;color:#fff;border:0;border-radius:5px;padding:9px 10px;margin:8px 5px 0 0;cursor:pointer}button:hover{background:#2184ba}.danger{background:#963f38}.preset{background:#31465d}.kpis{display:grid;grid-template-columns:repeat(3,minmax(120px,1fr));gap:10px}.kpi{background:#0a1523;border-radius:7px;padding:11px}.kpi span{display:block;font-size:12px;color:#a9bfd3}.kpi strong{font-size:20px}.status{border-radius:7px;padding:9px;margin-bottom:12px;background:#183d2a;color:#8ff5a8}.warn{background:#4f3320;color:#ffbe78}canvas{width:100%;height:260px;background:#081321;border-radius:7px}@media(max-width:760px){.grid{grid-template-columns:1fr}.kpis{grid-template-columns:repeat(2,1fr)}}
</style></head><body><main><h1>Digital Twin Control Room</h1><div class=""sub"">Simulation only — the dashboard is bound to this computer and never controls production equipment.</div><div id=""status"" class=""status"">Connecting…</div><div class=""grid""><section class=""panel""><b>SETPOINTS</b><label>Conveyor <output id=""convOut""></output><input id=""conv"" type=""range"" min=""0.2"" max=""2.5"" step=""0.01""></label><label>Pump flow <output id=""pumpOut""></output><input id=""pump"" type=""range"" min=""0"" max=""300"" step=""1""></label><label>Infeed turntable <output id=""rpmOut""></output><input id=""rpm"" type=""range"" min=""5"" max=""60"" step=""0.1""></label><button onclick=""apply()"">Apply</button><button id=""pause"" onclick=""togglePause()"">Pause</button><button class=""danger"" onclick=""send({type:'reset',newSeed:false})"">Reset</button><button class=""danger"" onclick=""send({type:'reset',newSeed:true})"">New seed + reset</button><hr><b>EXPERIMENT PRESETS</b><br><button class=""preset"" onclick=""preset('Nominal')"">Nominal</button><button class=""preset"" onclick=""preset('HighConveyor')"">High conveyor</button><button class=""preset"" onclick=""preset('LowPumpFlow')"">Low pump flow</button><button class=""preset"" onclick=""preset('HighInfeedRpm')"">High infeed RPM</button></section><section class=""panel""><div class=""kpis"" id=""kpis""></div><h3>Last 60 seconds</h3><canvas id=""chart"" width=""780"" height=""260""></canvas><div class=""sub"">Blue: fill % · Red: reject % · Green: vessel % · Yellow: throughput</div></section></div></main>
<script>
const q=id=>document.getElementById(id), hist=[];let socket,snap={},applyingFromSnapshot=false,applyTimer;
q('rpm').parentElement.insertAdjacentHTML('afterend','<label>Disc index speed <output id=""discRpmOut""></output><input id=""discRpm"" type=""range"" min=""1"" max=""30"" step=""0.01""></label><label>Disc dwell <output id=""discDwellOut""></output><input id=""discDwell"" type=""range"" min=""0.1"" max=""5"" step=""0.01""></label>');
document.querySelector('button[onclick*=""HighInfeedRpm""]').insertAdjacentHTML('afterend','<br><button class=""preset"" onclick=""preset(&quot;FastDiscIndex&quot;)"">Fast Disc index</button><button class=""preset"" onclick=""preset(&quot;SlowDiscIndex&quot;)"">Slow Disc index</button><br><button class=""preset"" onclick=""preset(&quot;ShortDiscDwell&quot;)"">Short Disc dwell</button><button class=""preset"" onclick=""preset(&quot;LongDiscDwell&quot;)"">Long Disc dwell</button>');
function fmt(v,d=1){return Number(v||0).toFixed(d)}function send(o){if(socket&&socket.readyState===1)socket.send(JSON.stringify(o))}
function apply(){send({type:'setpoints',conveyorSpeedMps:+q('conv').value,pumpFlowLitersPerMinute:+q('pump').value,infeedMotorSpeedRpm:+q('rpm').value,starWheelIndexSpeedRpm:+q('discRpm').value,starWheelDwellSeconds:+q('discDwell').value})}
function queueApply(){clearTimeout(applyTimer);applyTimer=setTimeout(apply,80)}
function preset(p){send({type:'preset',preset:p})}function togglePause(){send({type:'pause',paused:!snap.paused})}
for(const id of ['conv','pump','rpm','discRpm','discDwell'])q(id).oninput=()=>{q(id+'Out').textContent=id==='conv'?fmt(q(id).value,2)+' m/s':id==='pump'?fmt(q(id).value,0)+' L/min':id==='discDwell'?fmt(q(id).value,2)+' s':fmt(q(id).value,2)+' rpm';if(!applyingFromSnapshot)queueApply()};
function connect(){socket=new WebSocket(`ws://${location.host}/ws`);socket.onopen=()=>q('status').textContent='Connected to local Unity simulation';socket.onclose=()=>{q('status').textContent='Reconnecting…';setTimeout(connect,1000)};socket.onmessage=e=>{snap=JSON.parse(e.data);render()}}
function render(){const s=snap;if(hist.length&&s.simulationSeconds<hist[hist.length-1].t)hist.length=0;if(document.activeElement!==q('conv'))q('conv').value=s.conveyorSpeedMps||0;if(document.activeElement!==q('pump'))q('pump').value=s.pumpFlowLitersPerMinute||0;if(document.activeElement!==q('rpm'))q('rpm').value=s.infeedMotorSpeedRpm||0;['conv','pump','rpm'].forEach(id=>q(id).oninput());q('pause').textContent=s.paused?'Resume':'Pause';const warn=s.alert!=='Normal';q('status').textContent='STATUS: '+s.alert;q('status').className=warn?'status warn':'status';const cards=[['Throughput',fmt(s.throughputBottlesPerHour,0)+' bottles/h'],['Average fill',fmt(s.averageFillPercent)+'%'],['Last batch',fmt(s.lastBatchFillPercent)+'%'],['Reject rate',fmt(s.rejectRatePercent)+'%'],['Vessel',fmt(s.vesselLevelLiters)+' / '+fmt(s.vesselCapacityLiters,0)+' L'],['Buffer',s.turntableBufferCount+' | line '+s.bottlesOnConveyorCount],['Pass / reject',s.totalPassed+' / '+s.totalRejected],['Turntable ω',fmt(s.angularSpeedRadPerSec,2)+' rad/s'],['Centrifugal a',fmt(s.centrifugalAccelerationMps2,2)+' m/s²']];q('kpis').innerHTML=cards.map(c=>`<div class='kpi'><span>${c[0]}</span><strong>${c[1]}</strong></div>`).join('');hist.push({t:s.simulationSeconds,fill:s.averageFillPercent,reject:s.rejectRatePercent,vessel:100*s.vesselLevelLiters/Math.max(1,s.vesselCapacityLiters),through:s.throughputBottlesPerHour});while(hist.length&&hist[0].t<s.simulationSeconds-60)hist.shift();draw()}
const baseRender=render;render=function(){applyingFromSnapshot=true;baseRender();const s=snap;if(document.activeElement!==q('discRpm'))q('discRpm').value=s.starWheelIndexSpeedRpm||0;if(document.activeElement!==q('discDwell'))q('discDwell').value=s.starWheelDwellSeconds||0;q('discRpm').oninput();q('discDwell').oninput();applyingFromSnapshot=false;q('kpis').insertAdjacentHTML('beforeend',`<div class='kpi'><span>Reject escapes</span><strong>${s.totalRejectEscapes||0}</strong></div><div class='kpi'><span>Disc</span><strong>${fmt(s.starWheelIndexSpeedRpm,2)} rpm | ${fmt(s.starWheelDwellSeconds,2)} s</strong></div><div class='kpi'><span>Disc index</span><strong>${fmt(s.starWheelIndexDurationSeconds,2)} s</strong></div>`) };
function draw(){const c=q('chart'),x=c.getContext('2d'),w=c.width,h=c.height;x.clearRect(0,0,w,h);x.strokeStyle='#1c3850';for(let i=1;i<5;i++){x.beginPath();x.moveTo(0,i*h/5);x.lineTo(w,i*h/5);x.stroke()}const end=hist.length?hist[hist.length-1].t:1,start=Math.max(0,end-60),maxT=Math.max(100,...hist.map(v=>v.through));function line(key,max,color){if(hist.length<2)return;x.strokeStyle=color;x.lineWidth=2;x.beginPath();hist.forEach((v,i)=>{const px=(v.t-start)/60*w,py=h-8-(v[key]/max)*(h-18);i?x.lineTo(px,py):x.moveTo(px,py)});x.stroke()}line('fill',100,'#35caff');line('reject',100,'#ff5d4d');line('vessel',100,'#66ed8a');line('through',maxT,'#ffd34a')}
connect();
</script></body></html>";
    }
}
