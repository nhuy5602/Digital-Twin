# Digital Twin: Filling & Filtering Line

Project Unity mô phỏng dây chuyền **filling & filtering** chai nước ở mức Digital Model / Digital Shadow. Mô hình tập trung vào luồng chai thực tế: cấp chai bằng turntable, đưa vào star wheel để rót và đóng nắp, kiểm tra QC, loại chai lỗi, rồi gom chai đạt vào accumulation turntable cuối line để đóng thùng.

Scene chính:

```text
Assets/Scenes/SampleScene.unity
```

Script chính:

```text
Assets/Scripts/FillingFilteringDigitalTwin.cs
Assets/Scripts/BottleProcessState.cs
Assets/Scripts/FillingFilteringHud.cs
Assets/Scripts/ConveyorDemoRuntimeBootstrap.cs
```

## Cách chạy demo

1. Mở Unity Hub.
2. Add project từ thư mục:

```text
D:\Học thạc sĩ\IT6019\Conveyor-Digital-Twin
```

3. Mở scene:

```text
Assets > Scenes > SampleScene
```

4. Nếu muốn dựng lại toàn bộ scene tự động, chọn menu:

```text
Tools > Conveyor Twin > Build Demo Scene
```

5. Bấm **Play**.

Khi chạy, chai sẽ rơi vào infeed turntable, ra conveyor hẹp dạng slat chain, vào star wheel để fill và đóng nắp, đi qua QC, chai lỗi bị piston đẩy khỏi line rồi ẩn khỏi scene, chai đạt đi tiếp vào accumulation turntable cuối line và được chuyển vào thùng carton theo batch.

## 1. Bottle Infeed Station

Thiết bị:

```text
Infeed Turntable
Bottle Dropper
Turntable Outlet
```

Logic vận hành:

- Chai rỗng được sinh ra từ `Bottle Spawn Point` phía trên mâm.
- Chai rơi xuống `Infeed Turntable`.
- Turntable hoạt động như buffer/caching table: chai tích lại trên mâm, motor xoay mâm, chai dạt dần ra rìa barrier.
- Khi buffer đủ `releaseThreshold` và chai đi đến vùng outlet, hệ thống xả từng chai qua `Turntable Outlet`.
- Chai từ outlet đi vào line hẹp trước khi được star wheel bắt vào pocket.

Công thức vật lý mô phỏng lực ly tâm:

```text
omega = rpm * 2π / 60
a_c = omega^2 * r
```

Digital Twin Data:

- `Throughput`: năng suất chai/giờ.
- `Infeed Motor Speed`: tốc độ motor cấp liệu, đơn vị rpm.
- `Turntable Buffer`: số chai đang nằm trên mâm.
- `Centrifugal Acceleration`: gia tốc ly tâm tại rìa mâm.

## 2. Slat Chain Conveyor

Conveyor chính được dựng theo dạng **slat chain / modular conveyor**:

- bề rộng hẹp vừa kích thước chai,
- mặt băng gồm nhiều `Modular Slat Plate` chia khoảng đều theo `slatPitchM`,
- có `Slat Gap Shadow` để nhìn rõ khe giữa các mắt xích,
- có `Anti Slip Cross Rib` để giảm trượt bề mặt,
- rail dẫn hướng được khoét tại vùng piston reject để piston có khoảng đẩy chai lỗi.

Logic chống trượt:

```text
effectiveSpeed = conveyorSpeed * (1 - conveyorSlipRatio)
```

Trong demo, `conveyorSlipRatio = 0.02`, nghĩa là chai gần như bám theo slat chain và chỉ mất khoảng 2% vận tốc do trượt bề mặt. Khoảng cách chai trên conveyor được giữ theo pitch của star wheel để hạn chế chồng chai.

## 3. Filling Star Wheel

Thiết bị:

```text
10-pocket Filling Star Wheel
3 x Filling Nozzle
Liquid Vessel
Cap Dropper
3 x Rotary Capping Head
```

Logic vận hành:

- Chai đi thẳng trên conveyor, sau đó được bắt vào pocket của `Filling Star Wheel`.
- Star wheel có `10` pocket; mỗi bước index cơ bản là:

```text
stepAngle = 360 / 10 = 36°
pocketPitch = 2 * pi * starWheelPocketRadius / 10
```

- Cụm filling dùng `3` vòi rót đặt theo cung star wheel.
- Mỗi batch rót gồm 3 chai, tương ứng star wheel index 3 pocket trong chu kỳ filling.
- Khi batch đủ chai dưới vòi, hệ thống kích hoạt dòng chảy `Liquid Flow`.
- 90% chai được rót chuẩn `100%`, 10% chai bị underfilled `50-60%` để mô phỏng lỗi nghẹt vòi.
- Sau vùng fill, nắp được cấp bằng `Cap Dropper`.
- Trong phần sau của star wheel, 3 `Rotary Capping Head` được cố định lần lượt tại pocket 7, 8, 9.
- Starwheel dừng lần thứ nhất để fill 3 chai và dừng lần thứ hai để cả 3 đầu cap đóng nắp trước khi quay tiếp.
- Hết star wheel, chai rời pocket theo tiếp tuyến và quay lại conveyor thẳng để đi qua QC.

Digital Twin Data:

- `Liquid Level`: mức chất lỏng trong bồn tổng.
- `Filling Time`: thời gian rót gần nhất.
- `Bottles At Filling Station`: số chai đang ở vùng rót.
- `Bottles At Capping Station`: số chai đang ở vùng đóng nắp.
- `Star Wheel Locked`: trạng thái star wheel đang giữ chai trong batch fill/cap.

## 4. QC Sensor Station

Thiết bị:

```text
Photoelectric Sensor / Virtual Vision Sensor
```

Logic vận hành:

- Chai đi qua tia quét của cảm biến.
- Sensor đọc biến `liquidVolume01` của từng chai.
- Quy tắc kiểm tra:

```text
Volume >= 95%  => PASSED
Volume < 95%   => REJECTED
```

Digital Twin Data:

- `Inspection Status = Normal` nếu chai đạt.
- `Inspection Status = AnomalyDetected` nếu chai thiếu nước.

## 5. Sorting & Rejection Station

Thiết bị:

```text
Pneumatic Pusher
Rejected Bottle Removal
```

Logic vận hành:

- Chai `PASSED` tiếp tục chạy thẳng đến cuối conveyor.
- Chai `REJECTED` khi đến vị trí piston sẽ gọi logic `TriggerPusher()`.
- Piston đỏ lao ra qua phần rail đã khoét, chai lỗi được ẩn khỏi scene ngay sau cú đẩy, sau đó piston co về.
- Không tạo `Reject Chute` trong scene.

Digital Twin Data:

- `Total Rejected`: tổng chai lỗi.

## 6. Accumulation & Carton Packing Station

Thiết bị:

```text
Accumulation Turntable
Accumulation Inlet Counting Sensor
Inlet Gate
Outlet Gate
Carton Box
Carton Discharge Pusher
```

`Accumulation Turntable` uses the same geometry and dimensions as `Infeed Turntable`; only its station position and process role are different.

Logic vận hành:

- Chai đạt sau QC đi đến cảm biến trước cổng vào accumulation turntable.
- Sensor tăng `AccumulationEntryCount` mỗi khi một chai đi vào.
- Chai được đưa vào `Accumulation Turntable` và xoay tích trữ như một buffer cuối line.
- Chai trong turntable được quay và dạt dần ra ngoài theo lực ly tâm `a_c = omega^2 * r`.
- Chỉ khi chai đi tới cửa outlet và rơi vào `Active Carton Box` thì mới tăng `Total Passed` và bộ đếm carton.
- Khi đủ `accumulationBatchSize = 6` chai đã rơi vào thùng, `Inlet Gate` đóng, `Carton Discharge Pusher` đẩy thùng đầy ra ngoài.
- Hệ thống reset thùng rỗng mới rồi mở lại cổng vào.

Digital Twin Data:

- `Accumulation Buffer`: số chai đang chờ trong turntable cuối.
- `Sensor Count`: tổng số chai đã vào turntable cuối.
- `Cartons`: số thùng carton đã đóng xong.
- `Total Passed`: tổng chai đạt.

## Dashboard

HUD trong game hiển thị:

- Throughput, bottles/hour.
- Infeed motor speed, rpm.
- Turntable buffer và số chai trên conveyor.
- `omega` và `a_c rim` của turntable.
- Slat pitch và slip ratio của conveyor.
- Số chai ở filling/capping.
- Vessel liquid level và filling time.
- Inspection status.
- Accumulation buffer, inlet/outlet gate state, cartons filled.
- Total passed / rejected.

## Công thức và logic mô phỏng

Năng suất:

```text
Throughput = completedBottleCount / elapsedTimeHours
```

Tỉ lệ chất lỏng trong chai:

```text
liquidVolume01 = currentVolume / bottleCapacity
```

Quy tắc QC:

```text
if liquidVolume01 >= 0.95:
    status = PASSED
else:
    status = REJECTED
```

Mức chất lỏng bồn tổng:

```text
LiquidLevel = LiquidLevel - filledVolume * bottleCapacityLiters
```

Logic xác suất lỗi:

```text
90% => volume = 1.0
10% => volume = random(0.5, 0.6)
```

Logic star wheel + filling:

```text
capture bottle into star wheel pocket
index pocket along star wheel arc
if 3 bottles are under filling nozzles:
    lock star wheel
    fill all 3 bottles in parallel
after filling:
    index 3 pockets to capping pockets 7, 8, 9
    stop star wheel and tighten all 3 bottles in parallel
release bottle back to straight conveyor
```

Logic accumulation cuối line:

```text
if capped passed bottle reaches accumulation sensor:
    move bottle into accumulation turntable
while bottle is inside accumulation turntable:
    rotate turntable
    radius += centrifugal_acceleration * dt
    if radius reaches outlet and bottle falls into carton:
        count passed bottle
if carton bottle count >= 6:
    close inlet gate
    push full carton away
    reset empty carton
```

## File quan trọng

- `FillingFilteringDigitalTwin.cs`: điều phối toàn bộ quy trình.
- `BottleProcessState.cs`: lưu trạng thái từng chai, volume, pass/reject/capped.
- `FillingFilteringHud.cs`: dashboard digital twin.
- `ConveyorDemoRuntimeBootstrap.cs`: tự dựng scene demo khi mở/chạy scene.
- `ConveyorDemoSceneBuilder.cs`: menu editor để dựng lại scene.
