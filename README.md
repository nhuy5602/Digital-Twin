# Digital Twin: Filling & Filtering Line

Project Unity mô phỏng quy trình **filling & filtering** cho dây chuyền chai nước.

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

4. Nếu muốn dựng lại scene tự động, chọn menu:

```text
Tools > Conveyor Twin > Build Demo Scene
```

5. Bấm **Play**.

Khi chạy, các chai sẽ đi qua infeed, trạm chiết rót, trạm QC, trạm phân loại. Chai đạt sẽ đi thẳng xuống Accept Chute. Chai lỗi sẽ bị Pneumatic Pusher đẩy sang Reject Chute.

## 1. Bottle Infeed Station

Thiết bị sử dụng:

```text
Infeed Turntable
```

Logic vận hành:

- Chai rỗng được sinh ra từ `Bottle Spawn Point` phía trên mâm.
- Chai rơi xuống `Infeed Turntable`.
- Khi bấm Play, turntable được nạp sẵn `initialTurntableBottleCount = 12` chai để không bị trống lúc bắt đầu.
- Sau đó hệ thống chỉ sinh thêm chai từ trên xuống khi buffer thiếu chai.
- Mâm quay tự xoay tại chỗ như một buffer/caching table.
- Mô hình dùng công thức lực ly tâm gần đúng để đẩy chai dạt ra rìa barrier:

```text
omega = rpm * 2π / 60
a_c = omega^2 * r
```

- Khi chai sát rìa barrier và đi vào vùng outlet, hệ thống tự xả chai qua `Turntable Outlet`.
- Điều kiện xả chỉ bật khi số chai trong buffer đạt `releaseThreshold`.
- Chai sau đó đi vào conveyor chính để đến trạm filling.

Digital Twin Data:

- `Throughput`: năng suất chai/giờ.
- `Infeed Motor Speed`: tốc độ mô-tơ cấp liệu, đơn vị rpm.
- `Turntable Buffer`: số chai đang nằm trên mâm chờ ra outlet.
- `Centrifugal Acceleration`: gia tốc ly tâm tại rìa mâm.

## Conveyor chính

Conveyor chính được dựng theo dạng **slat chain / modular conveyor**:

- bề rộng được thu hẹp vừa kích thước chai,
- mặt băng gồm nhiều tấm `Modular Slat Plate` chia khoảng đều theo `slatPitchM`,
- giữa các slat có `Slat Gap Shadow` để nhìn rõ khe chia như slat chain thật,
- có `Anti Slip Cross Rib` đặt cách quãng để giảm trượt bề mặt,
- hai ray dẫn hướng giữ chai đi đúng một hàng,
- chai từ outlet của turntable đi vào giữa conveyor.

Logic chống trượt:

```text
effectiveSpeed = conveyorSpeed * (1 - conveyorSlipRatio)
```

Trong demo, `conveyorSlipRatio = 0.02`, nghĩa là chai gần như bám theo slat chain và chỉ mất khoảng 2% vận tốc do trượt bề mặt. Chai cũng được giữ khoảng cách tối thiểu `minimumBottleSpacingM` để không chồng lên nhau trên conveyor hẹp.

## 2. Filling Station

Thiết bị sử dụng:

```text
Filling Nozzle
Liquid Vessel
```

Logic vận hành:

- Khi chai đến `Filling Trigger Zone`, chai tạm dừng tại vị trí vòi rót.
- Vòi phun kích hoạt dòng chảy.
- Chai được rót trong thời gian `fillingTimeSeconds`.
- Sau khi rót xong, chai tiếp tục chạy sang trạm QC.

Logic lỗi tạo điểm nhấn:

- 90% chai được rót chuẩn: `Properly Filled = 100%`.
- 10% chai bị lỗi thiếu nước do nghẹt vòi: `Underfilled = 50-60%`.

Digital Twin Data:

- `Liquid Level`: mức chất lỏng còn lại trong bồn tổng.
- `Filling Time`: thời gian rót gần nhất.

## 3. QC Sensor Station

Thiết bị sử dụng:

```text
Photoelectric Sensor / Virtual Vision Sensor
```

Logic vận hành:

- Chai đi qua tia quét của cảm biến.
- Cảm biến đọc biến `liquidVolume01` của từng chai.
- Quy tắc kiểm tra:

```text
Volume >= 95%  => PASSED
Volume < 95%   => REJECTED
```

Digital Twin Data:

- `Inspection Status = Normal` nếu chai đạt.
- `Inspection Status = AnomalyDetected` nếu phát hiện chai thiếu nước.

## 4. Sorting & Rejection Station

Thiết bị sử dụng:

```text
Pneumatic Pusher
Sliding Chute
Accept Chute
Reject Chute
```

Logic vận hành:

- Chai `PASSED` tiếp tục chạy thẳng đến cuối băng tải và trượt xuống `Accept Chute`.
- Chai `REJECTED` khi đến vị trí piston sẽ gọi logic `TriggerPusher()`.
- Piston lao ra, đẩy chai lỗi sang `Reject Chute`, sau đó co về.

Digital Twin Data:

- `Total Passed`: tổng chai đạt.
- `Total Rejected`: tổng chai lỗi.

## Dashboard

HUD trong game hiển thị:

- Infeed throughput, bottles/hour.
- Infeed motor speed, rpm.
- Turntable buffer và số chai trên conveyor.
- `omega` và `a_c rim` của turntable.
- Slat pitch và slip ratio của conveyor.
- Vessel liquid level, L.
- Filling time, s.
- Inspection status.
- Total passed.
- Total rejected.
- Rule kiểm tra: `volume >= 95%`.

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

Logic turntable buffer:

```text
prefill initial bottles on turntable
if buffer has room:
    spawn extra bottle from above
    drop to turntable
omega = rpm * 2π / 60
a_c = omega^2 * r
bottle moves outward to rim barrier
if bufferCount >= releaseThreshold and bottle reaches outlet sector:
    move one bottle to outlet
    send bottle to conveyor
```

## File quan trọng

- `FillingFilteringDigitalTwin.cs`: điều phối toàn bộ quy trình.
- `BottleProcessState.cs`: lưu trạng thái từng chai, volume, pass/reject.
- `FillingFilteringHud.cs`: dashboard digital twin.
- `ConveyorDemoRuntimeBootstrap.cs`: tự dựng scene demo khi mở/chạy scene.
- `ConveyorDemoSceneBuilder.cs`: menu editor để dựng lại scene.
