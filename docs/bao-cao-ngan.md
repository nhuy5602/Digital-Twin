# Báo cáo ngắn: Digital Twin quy trình Filling & Filtering

## 1. Mục tiêu

Xây dựng mô hình digital twin cho dây chuyền chiết rót và kiểm tra chất lượng chai nước. Mô hình thể hiện bốn trạm chính: cấp chai, chiết rót, kiểm tra chất lượng và phân loại loại biên.

## 2. Mức độ digital twin

Mô hình đạt mức **Digital Shadow** vì dữ liệu vận hành được mô phỏng và cập nhật một chiều vào mô hình Unity:

- năng suất chai/giờ,
- tốc độ mô-tơ cấp chai,
- mức chất lỏng trong bồn,
- thời gian rót,
- trạng thái kiểm tra QC,
- tổng chai đạt,
- tổng chai lỗi.

## 3. Quy trình

### Bottle Infeed Station

Thiết bị: `Infeed Turntable`.

Mâm quay mô phỏng việc cấp chai rỗng vào băng chuyền theo thứ tự. Dữ liệu theo dõi gồm throughput và tốc độ mô-tơ cấp liệu.

Trong bản mô phỏng mới, chai được sinh ra từ phía trên mâm và rơi xuống turntable. Turntable đóng vai trò buffer/caching table: chai nằm trên mâm sẽ xoay quanh tâm mâm, khi số lượng chai trong buffer đạt ngưỡng thì hệ thống xả từng chai ra outlet để đi vào conveyor chính.

Logic infeed có áp dụng mô phỏng vật lý gần đúng:

```text
omega = rpm * 2π / 60
a_c = omega^2 * r
```

Trong đó `omega` là vận tốc góc của mâm quay, `r` là khoảng cách từ chai đến tâm mâm, và `a_c` là gia tốc ly tâm. Gia tốc này làm chai dạt dần ra barrier ngoài; khi chai đi tới sector outlet thì được truyền sang conveyor.

### Filling Station

Thiết bị: `Filling Nozzle` và `Liquid Vessel`.

Khi chai đến vùng rót, chai dừng lại trong thời gian `fillingTimeSeconds`. Hệ thống mô phỏng:

- 90% chai được rót đủ 100% dung tích,
- 10% chai bị underfilled, chỉ đạt 50-60% dung tích.

### QC Sensor Station

Thiết bị: cảm biến quang/vision sensor ảo.

Quy tắc kiểm tra:

```text
Volume >= 95% => PASSED
Volume < 95%  => REJECTED
```

### Sorting & Rejection Station

Thiết bị: `Pneumatic Pusher`, `Accept Chute`, `Reject Chute`.

Chai đạt đi thẳng xuống máng accept. Chai lỗi đến vị trí piston sẽ được pusher đẩy sang máng reject.

## 4. Công thức và logic

Năng suất:

```text
Throughput = completedBottleCount / elapsedTimeHours
```

Mức đầy của chai:

```text
liquidVolume01 = currentVolume / bottleCapacity
```

Mức chất lỏng bồn tổng:

```text
LiquidLevel = LiquidLevel - filledVolume * bottleCapacityLiters
```

Logic xác suất:

```text
P(properly filled) = 0.9
P(underfilled) = 0.1
```

## 5. Thành phần Unity

- `FillingFilteringDigitalTwin.cs`: điều phối logic dây chuyền.
- `BottleProcessState.cs`: lưu thể tích, trạng thái và kết quả QC của từng chai.
- `FillingFilteringHud.cs`: hiển thị dashboard.
- `ConveyorDemoRuntimeBootstrap.cs`: tự dựng mô hình trực quan trong scene.
- `ConveyorDemoSceneBuilder.cs`: menu editor để dựng lại scene.

## 6. Logic turntable buffer

```text
spawn bottle from above
drop bottle into turntable
omega = rpm * 2π / 60
a_c = omega^2 * r
bottle moves outward to rim barrier
if bufferCount >= releaseThreshold and bottle reaches outlet sector:
    release one bottle through outlet
    transfer bottle to conveyor
```

Dữ liệu hiển thị thêm trên HUD:

- số chai trong turntable buffer,
- số chai đang ở conveyor/line xử lý.
- vận tốc góc turntable,
- gia tốc ly tâm tại rìa mâm.

## 7. Conveyor

Conveyor chính được đổi sang dạng **slat chain/modular conveyor**. Băng tải được thu hẹp theo kích thước chai và gồm nhiều tấm `Modular Slat Plate`, hai bên có guide rail để giữ chai chạy một hàng.
