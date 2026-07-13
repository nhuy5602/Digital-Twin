# Digital Model: Filling & Filtering Line

Mô hình Unity mô phỏng một dây chuyền chai nước gồm cấp chai, chiết rót, đóng nắp, kiểm tra chất lượng, loại chai lỗi, chia hai lane và đóng six-pack. Tài liệu này là nguồn mô tả chuẩn của project; tài liệu rút gọn và chuyên sâu được liên kết tại [docs/bao-cao-ngan.md](docs/bao-cao-ngan.md) và [docs/scalloped-star-wheel-logic.md](docs/scalloped-star-wheel-logic.md).

## 1. Phạm vi Digital Twin

Ở trạng thái hiện tại, đây là **Digital Model**: trạng thái chai, máy, chất lỏng và chỉ số vận hành được sinh và tính trong Unity. Scene Filling & Filtering chưa nhận tín hiệu từ PLC, cảm biến, IIoT hoặc tệp CSV thật, vì vậy không nên gọi nó là Digital Shadow hay Digital Twin kết nối hai chiều.

Các script `ConveyorBeltDigitalTwin`, `TwinTelemetrySimulator` và `CsvTelemetryPlayer` là hạ tầng tổng quát có thể dùng để mở rộng sang Digital Shadow. Để đạt cấp đó, cần gắn nguồn telemetry vào line hiện tại, định nghĩa mapping tag thiết bị, đồng bộ thời gian, và kiểm chứng sai số với dây chuyền thật.

Mục tiêu học thuật của mô hình là:

- minh họa luồng vật liệu và các trạng thái công nghệ của chai;
- cho phép quan sát ảnh hưởng của tốc độ mâm, băng tải, star wheel và thời gian rót/đóng nắp;
- trình bày rõ phần nào là công thức vật lý, phần nào là xấp xỉ động học phục vụ trực quan;
- tạo nền tảng cho virtual commissioning hoặc tích hợp telemetry ở bước sau.

## 2. Chạy demo

Yêu cầu: **Unity 6000.5.0f1**.

1. Mở thư mục gốc project này bằng Unity Hub.
2. Mở `Assets/Scenes/SampleScene.unity`.
3. Bấm **Play** để chạy mô phỏng.

Để dựng lại scene sinh bằng code, dùng menu **Tools > Conveyor Twin > Build Demo Scene**. Lệnh này xóa phần scene demo đã sinh, dựng lại và lưu đè `SampleScene.unity`; chỉ dùng khi đã lưu/commit các thay đổi scene cần giữ.

Trong Play Mode, HUD có slider cho tốc độ conveyor, tốc độ turntable, tốc độ index star wheel, thời gian rót, thời gian đóng nắp và khoảng cách xả chai từ mâm.

## 3. Luồng công nghệ

```text
Bottle Dropper
  -> Infeed Turntable
  -> Infeed Neck Support Rail Left + Air Blower
  -> 10-pocket Filling Star Wheel
  -> QC sensor
  -> Reject pusher hoặc A/B splitter
  -> hai hàng 3 chai
  -> six-pack carton
```

1. Chai rỗng được sinh phía trên mâm; điểm đáp có lệch theo chiều cấp chai để biểu diễn ảnh hưởng gió.
2. Trên turntable, chai quay theo mâm, dạt ra mép và giữ khoảng cách với chai khác.
3. Khi chạm `Infeed Neck Support Rail Left` ở vùng cửa ra, chai bị guide chặn theo hình học collider rồi chuyển mượt sang rail. `Turntable Outlet` chỉ là hình học cửa ra của mâm, **không** còn là trigger đưa chai đến một điểm cố định trên rail.
4. Air blower đẩy chai dọc rail. Chai vẫn giữ cao độ mặt mâm khi tâm còn trong bán kính mâm; chỉ hạ dần xuống cao độ rail ở phía ngoài mâm.
5. Chai đầu hàng, đủ gần entry pocket, được star wheel bắt vào. Ba chai được rót song song; sau đó group được index đến vùng cấp nắp và siết nắp.
6. Chai đã rót đi qua QC. Chai thiếu thể tích bị piston loại; chai đạt được chia luân phiên theo nhóm `A,A,A,B,B,B`, dồn thành lưới `3 x 2` và đẩy vào carton.

## 4. Cấu hình runtime quan trọng

Các giá trị dưới đây là giá trị do `ConveyorDemoRuntimeBootstrap` gán khi dựng demo; chúng ưu tiên hơn giá trị mặc định hiển thị ở Inspector.

| Hạng mục | Giá trị chạy demo | Ý nghĩa |
| --- | ---: | --- |
| Turntable | `18 rpm`, bán kính `0,95 m` | Tạo chuyển động quay và buffer chai. |
| Chai trên mâm | bán kính logic `0,11 m` | Dùng cho giới hạn biên và tách chai. |
| Buffer | đầu `12`, tối đa `16`, ngưỡng xả `7` | Điều tiết cấp chai vào rail. |
| Rơi chai | `0,45 s`, lệch gió `0,18 m` | Chuyển động trực quan tới mặt mâm. |
| Chuyển sang rail | `0,14 s` | Nội suy ngang mượt khi guide bắt chai. |
| Hạ ngoài mâm | `0,22 m` | Quãng đường chuyển cao độ từ mâm xuống rail. |
| Conveyor | `0,85 m/s`, slip runtime `0` | Chai bám lý tưởng theo băng tải trong demo. |
| Star wheel | `10` pocket, index cơ sở `36°` | `360° / 10`. |
| Rót / cap | `3` vòi / `3` đầu | Rót theo batch ba chai; cap trong star wheel. |
| Chất lượng | `P(đủ) = 0,9`, pass `>= 0,95` | Chai lỗi được rót trong khoảng `0,5–0,6`. |

## 5. Mô hình vật lý và xấp xỉ thực thi

### 5.1 Turntable: quay, bám và dạt chai

Với tốc độ quay `rpm`, vận tốc góc là:

```text
ω = 2π · rpm / 60                         [rad/s]
v_t = r · ω                               [m/s]
a_c = r · ω² = v_t² / r                  [m/s²]
```

Trong cơ học quán tính, `a_c` là **gia tốc hướng tâm**, hướng vào tâm. Lực ma sát/bám giữa đáy chai và mặt mâm là lực có thể tạo ra gia tốc này. Khi mô tả từ hệ quy chiếu quay cùng mâm, người quan sát thường gọi xu hướng dạt ra ngoài là hiệu ứng ly tâm.

Code mô phỏng hiệu ứng đó bằng hai thành phần trong mặt phẳng `X-Z`:

```text
a_out = r_hat · ω² · r
a_grip = (v_surface - v_bottle) · surfaceGrip
v_bottle(t + Δt) = damping · [v_bottle(t) + (a_out + a_grip) · Δt]
```

Đây là mô hình động học gần đúng, không giải đầy đủ ma sát Coulomb, khối lượng, mô-men lật chai hoặc tiếp xúc rigidbody. Nó phù hợp để biểu diễn chai quay theo mâm và dạt đến guide, không dùng để dự báo lực thực tế trên chai.

Tâm chai bị giới hạn trong bán kính `R_table - R_bottle`; chai trên mâm và chai đang ở đầu rail được tách theo khoảng cách tâm tối thiểu `2R_bottle`. Khi rail đầy, chai trên mâm bị guide chặn và chờ chỗ trống thay vì xuyên qua chai rail.

### 5.2 Rơi chai và lệch gió

Chai rơi từ `Bottle Spawn Point` tới điểm đáp bằng nội suy tuyến tính theo `bottleDropTimeSeconds`. Điểm đáp được dịch về phía rail bằng `bottleDropWindDriftM = 0,18 m`.

```text
p(t) = lerp(p_start, p_target, t / T)
```

Vì không có phương trình `y = y_0 + v_0t - 1/2gt²`, lực cản hay va chạm đất, đây **không phải** quỹ đạo rơi tự do. “Gió” hiện là độ lệch có kiểm soát của điểm đáp để mô hình dễ quan sát và ổn định.

### 5.3 Guide rail, hàng chờ và air blower

`Infeed Neck Support Rail Left` được truyền vào process dưới dạng collider thật. Khi chai turntable chạm collider tại vùng dẫn hướng, logic triệt thành phần vận tốc đi vào rail, giải chồng lấn với chai đầu rail và chỉ cho chuyển trạng thái khi còn vị trí nhận.

Trạng thái infeed của mỗi chai được lưu rõ bằng `InfeedBottleState`:

```text
DroppingToTurntable -> OnTurntable -> TransitioningToNeckRail
-> OnNeckRail -> OnStarWheel
```

Việc tách trạng thái tránh suy luận sai “chai ở rail” chỉ bằng tọa độ `z`. `TransitioningToNeckRail` nội suy vị trí trong `0,14 s`; chai chỉ bắt đầu trượt rail sau khi chuyển xong.

Trên rail, tốc độ được cập nhật theo dạng bão hòa:

```text
v_target = clamp(v_min + v_wind, v_min, v_max)
a_feed   = a_rail + v_wind · gain
v_next   = MoveTowards(v_now, v_target, a_feed · Δt)
```

`neckRailGravityAccelerationMps2` trong code là **gia tốc cấp liệu hiệu dụng**. Về vật lý, thành phần trọng lực trên rail nghiêng sẽ là `g·sin(θ)`; nhưng rail runtime hiện gần như ngang, nên tham số này không được hiểu là phép đo trực tiếp của `g·sin(θ)`. Nó gom hiệu ứng dốc giả định, rung động và lực dẫn hướng để giữ dòng chai ổn định.

Air blower đặt tại `(-2,517; 2,000; -0,892)`. Bốn jet được dựng song song theo `+X`, chúc xuống `15°`. Các jet là minh họa hướng khí; code không giải trường áp suất, vận tốc khí hay CFD. Tác động động học của gió nằm ở `airBlowerWindSpeedMps` và `airBlowerAccelerationGain`.

Khoảng cách hàng rail được giới hạn theo tiến độ dọc rail (`ResolveInfeedRailSpacing`), tức là một hàng đợi xác định chứ không phải solver va chạm tròn đầy đủ. Vì thế không nên dùng mô hình để tính lực nén thực tế giữa các chai.

### 5.4 Hạ cao độ ngoài turntable

Gọi `d` là khoảng cách `X-Z` từ tâm chai đến tâm mâm:

```text
d = ||p_xz - c_xz||
u = clamp01((d - R_table) / L_drop)
s(u) = 3u² - 2u³
y = (1 - s) · y_table + s · y_rail
```

Với `R_table = 0,95 m` và `L_drop = 0,22 m`, chai giữ `y_table` khi `d <= R_table`, rồi hạ mượt và đạt `y_rail` khi `d >= R_table + 0,22`. `SmoothStep` làm vận tốc hạ bằng không ở đầu/cuối đoạn chuyển, nên không tạo snap nhìn thấy được.

### 5.5 Slat chain conveyor

Mô hình tổng quát dùng:

```text
v_effective = v_conveyor · (1 - slipRatio)
```

Runtime demo gán `slipRatio = 0`, nghĩa là chai đi theo băng tải lý tưởng ở `0,85 m/s`. Giá trị mặc định `0,02` trong component là tham số tổng quát, không phải cấu hình scene đang dựng. Khoảng cách chai trên conveyor được kiểm soát bằng logic vị trí, không dùng tiếp xúc vật lý với từng slat.

### 5.6 Star wheel, chiết rót và đóng nắp

Star wheel có `N = 10` pocket:

```text
Δθ = 360° / N = 36°
pitch_arc = 2πR / N
```

Chai chỉ được bắt khi là chai đầu rail và nằm trong cửa bắt sát entry pocket. Wheel có thể index một pocket để nạp dần; khi đủ ba chai ở pocket rót, ba vòi hạ xuống, dòng chất lỏng hiển thị và ba chai được rót song song. Sau batch rót, wheel index ba pocket để đưa group về các công đoạn sau.

Thể tích chuẩn hóa của chai là `liquidVolume01`. Trong thời gian rót, code tăng tuyến tính tới thể tích mục tiêu:

```text
V_bottle(t) = lerp(0, V_target, t / T_fill)
ΔL_vessel = Σ(ΔV_bottle · bottleCapacityLiters)
```

90% chai nhận `V_target = 1`; 10% nhận ngẫu nhiên `0,5–0,6`. Đây là mô hình thể tích xác định, không mô phỏng lưu lượng qua van, áp suất bồn hay động lực học chất lỏng.

Nắp được cấp từ magazine tại pocket cap-drop. Ba đầu capping được căn về các pocket capping, hạ xuống, quay trực quan rồi đặt `cappingCompleted = true`. Đóng nắp nằm trong star wheel, trước QC.

### 5.7 QC, reject, splitter và six-pack

QC áp dụng quy tắc:

```text
liquidVolume01 >= 0,95  => PASSED
liquidVolume01 <  0,95  => REJECTED
```

Chai reject bị pneumatic pusher đẩy sang bên, được đếm rồi ẩn khỏi scene. Chai đạt đã cap được gán lane theo chuỗi `A,A,A,B,B,B`.

Trước khi guide đổi lane, code ước lượng:

```text
t_lead = distance_to_guide / v_effective
t_available = (bottleSpacing - 2R_bottle - safetyGap) / v_effective
```

Nếu không đủ thời gian cho stroke guide, line tạm dừng qua safety interlock. Mỗi lane tích ba chai; khi hai lane đủ sáu chai, gate chặn lane, pusher đẩy group vào carton, carton và chai chuyển động bằng nội suy mượt. Các stroke này là animation điều khiển xác định, không tính lực khí nén hay biến dạng carton.

## 6. Dữ liệu quan sát và điều khiển

HUD hiển thị throughput, tốc độ mâm, số chai buffer/conveyor, `ω`, `a_c`, star-wheel phase, mức bồn, thời gian rót, QC, trạng thái splitter/gate, số chai pack, carton hoàn tất và tổng passed/rejected.

Throughput là chỉ số mô phỏng:

```text
throughput = completedBottleCount / elapsedTimeHours
```

Nó phụ thuộc vào thời điểm chạy demo, tốc độ slider và các hold logic; không phải năng suất đã được đo hay hiệu chuẩn từ một dây chuyền vật lý.

## 7. Giới hạn, kiểm chứng và hướng nâng cấp

- Không sử dụng `Rigidbody`, solver tiếp xúc 3D hay CFD; các va chạm/chuyển động quan trọng dùng trạng thái, collider guide và nội suy.
- Chai trên star wheel được đặt theo vị trí slot logic trong khi mesh wheel quay; chai chưa được parent vào pocket cơ khí thật.
- Chưa có calibration, dữ liệu thiết bị thật, phân tích độ bất định hoặc VVUQ. Vì vậy mô hình phù hợp minh họa, thử logic và virtual commissioning sơ bộ.
- Để nâng lên Digital Shadow: kết nối speed/level/count/quality thực, lưu timestamp, kiểm tra mapping telemetry, hiệu chuẩn tham số và so sánh kết quả simulation với quan sát thực.

Khi kiểm thử Play Mode, cần quan sát: chai không snap về base rail; chỉ hạ sau mép mâm; chai không xuyên qua guide/chai đầu rail; chai đầu rail mới được star wheel bắt; và six-pack chỉ hoàn tất khi đủ ba chai ở mỗi lane.

## 8. Thành phần mã nguồn

- `Assets/Scripts/FillingFilteringDigitalTwin.cs`: điều phối trạng thái chai, infeed, star wheel, filling, QC, splitter và packing.
- `Assets/Scripts/BottleProcessState.cs`: trạng thái công nghệ, trạng thái infeed, thể tích và visual của từng chai.
- `Assets/Scripts/ConveyorDemoRuntimeBootstrap.cs`: dựng hình học demo và gán cấu hình runtime.
- `Assets/Scripts/FillingFilteringHud.cs`: dashboard và điều khiển runtime.
- `Assets/Editor/ConveyorDemoSceneBuilder.cs`: menu dựng lại scene.

## 9. Tài liệu tham khảo

1. [ISO 23247-2:2021 — Digital twin framework for manufacturing, reference architecture](https://www.iso.org/cms/%20render/live/en/sites/isoorg/contents/data/standard/07/87/78743.html).
2. [NIST — Digital Twins for Advanced Manufacturing](https://www.nist.gov/programs-projects/digital-twins-advanced-manufacturing).
3. [OpenStax Physics — Key equations for angular velocity and centripetal acceleration](https://openstax.org/books/physics/pages/6-key-equations).
