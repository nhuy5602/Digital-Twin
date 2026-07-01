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

Trong bản mô phỏng mới, turntable được nạp sẵn một lượng chai ban đầu để thể hiện trạng thái buffer đang có hàng. Khi buffer thiếu chai, chai mới được sinh ra từ phía trên mâm và rơi xuống turntable. Turntable đóng vai trò buffer/caching table: chai nằm trên mâm sẽ xoay quanh tâm mâm, khi số lượng chai trong buffer đạt ngưỡng thì hệ thống xả từng chai ra outlet để đi vào conveyor chính.

Logic infeed có áp dụng mô phỏng vật lý gần đúng:

```text
omega = rpm * 2π / 60
a_c = omega^2 * r
```

Trong đó `omega` là vận tốc góc của mâm quay, `r` là khoảng cách từ chai đến tâm mâm, và `a_c` là gia tốc ly tâm. Gia tốc này làm chai dạt dần ra barrier ngoài; khi chai đi tới sector outlet thì được truyền sang conveyor.

### Filling Station

Thiết bị: cụm `Multiple Filling Nozzles`, `Liquid Vessel`, `Filling Stop Gate` và `Filling Star Wheel`.

Khi chai đến vùng rót, Star Wheel dạng đĩa tròn lớn có các pocket lõm quanh mép sẽ quay để index chai vào pocket và đưa đủ 4 chai vào đúng vị trí dưới 4 vòi. Gate phía trước vùng filling chặn các chai chưa đến lượt. Khi đủ chai, conveyor dừng toàn bộ, Star Wheel khóa chai cố định tại slot và 4 vòi rót đồng thời trong thời gian `fillingTimeSeconds`. Nếu turntable đã đủ buffer trong lúc conveyor dừng, turntable cũng dừng để tránh tiếp tục cấp chai.

Hệ thống mô phỏng:

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

### Capping Station

Thiết bị: `Capping Head`, `Cap Feeder Bowl`, `Cap Feed Rail`.

Chỉ chai đạt chuẩn mới đi tới trạm đóng nắp. Chai lỗi bị loại trước đó tại reject station. Khi chai đạt đến vị trí `cappingZ`, capping head hạ xuống, nắp chai được bật hiển thị, chai chuyển trạng thái `CAPPED` rồi đi tiếp xuống accept chute.

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

Logic filling nhiều vòi:

```text
assign bottle to filling slot
star wheel indexes bottle into pocket
when 4 bottles reached 4 nozzles:
    stop conveyor
    lock star wheel
    close filling gate
    snap bottles to fixed nozzle positions
    fill all bottles in parallel
    restart conveyor
```

## 5. Thành phần Unity

- `FillingFilteringDigitalTwin.cs`: điều phối logic dây chuyền.
- `BottleProcessState.cs`: lưu thể tích, trạng thái và kết quả QC của từng chai.
- `FillingFilteringHud.cs`: hiển thị dashboard.
- `ConveyorDemoRuntimeBootstrap.cs`: tự dựng mô hình trực quan trong scene.
- `ConveyorDemoSceneBuilder.cs`: menu editor để dựng lại scene.

## 6. Logic turntable buffer

```text
prefill initial bottles on turntable
if buffer has room:
    spawn extra bottle from above
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

Các tấm slat được chia khoảng theo `slatPitchM`, có khe `Slat Gap Shadow` và các gờ `Anti Slip Cross Rib`. Logic chuyển động dùng hệ số trượt nhỏ:

```text
effectiveSpeed = conveyorSpeed * (1 - conveyorSlipRatio)
```

Trong demo, `conveyorSlipRatio = 0.02`, giúp chai gần như bám theo slat chain và ít bị trượt bề mặt.
