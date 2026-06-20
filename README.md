# Digital Twin Bang Chuyen Hang Hoa

Du an nay la khung mo phong băng chuyền hàng hóa theo hai mức:

- **Digital Model**: mô hình chạy bằng tham số nhập tay như tốc độ băng, tải trọng, khối lượng băng, ma sát.
- **Digital Shadow**: mô hình nhận dữ liệu telemetry một chiều từ cảm biến/nguồn dữ liệu. Trong bản mẫu, `TwinTelemetrySimulator` giả lập dữ liệu cảm biến theo thời gian.

Project Unity hoàn chỉnh nằm ngay trong repo này. Scene demo chính là:

```text
Assets/Scenes/SampleScene.unity
```

Các script chính nằm trong:

```text
Assets/Scripts
```

## Chay demo nhanh trong Unity

1. Mở Unity Hub.
2. Chọn **Add project from disk**.
3. Chọn thư mục project:

```text
D:\Học thạc sĩ\IT6019\Conveyor-Digital-Twin
```

4. Mở project bằng Unity `6000.5.0f1` hoặc Unity 6 tương đương.
5. Trong Unity, mở scene:

```text
Assets > Scenes > SampleScene
```

6. Bấm nút **Play**.

Trong scene đã có object:

```text
Conveyor Demo Bootstrap
```

Object này gắn script `ConveyorDemoRuntimeBootstrap`. Khi scene được mở hoặc khi bấm **Play**, script sẽ tự tạo mô hình demo gồm băng chuyền, puly, kiện hàng, sensor, nguồn telemetry và HUD.

Nếu scene chưa hiện mô hình băng chuyền trong cửa sổ Scene/Game, vào menu:

```text
Tools > Conveyor Twin > Build Demo Scene
```

Menu này dùng script `ConveyorDemoSceneBuilder` để dựng và lưu lại scene demo. Sau đó bấm **Play** lại.

## Scene demo co gi

Scene đã dựng sẵn các đối tượng:

- `Conveyor Belt - Digital Shadow`: mặt băng chuyền chính.
- `Telemetry Source - Digital Shadow`: nguồn dữ liệu giả lập cảm biến.
- `HUD - Twin Metrics`: bảng thông số digital twin khi chạy.
- `Input Sensor` và `Output Sensor`: cảm biến đếm kiện hàng.
- `Package 01` đến `Package 05`: kiện hàng có Rigidbody và mô phỏng ma sát.
- Các cube hàng hóa tự chạy dọc theo băng chuyền. Khi tới cuối băng, chúng tự quay lại đầu băng để demo chạy liên tục.
- `Input Pulley`, `Output Pulley`: puly hai đầu băng chuyền.
- chân đỡ, ray đỡ và sàn nhà xưởng.

Khi bấm **Play**, HUD sẽ hiển thị:

- chế độ `DigitalShadow`,
- tốc độ băng chuyền,
- vận tốc góc puly,
- tải trọng,
- lực kéo,
- công suất mô hình và công suất đo,
- năng suất kiện/giờ,
- trạng thái `NORMAL`, `OVERLOAD`, `OVERSPEED` hoặc `EMERGENCY STOP`.

## Chuyen dong kien hang

Script `ConveyorPackage` có chế độ demo:

```text
forceKinematicMotion = true
loopOnBelt = true
startZ = -2.75
endZ = 2.75
```

Ở chế độ này, kiện hàng chạy theo tốc độ telemetry của băng chuyền. Công thức vật lý vẫn được dùng để ước lượng trượt:

```text
F_required = m * a
F_max = mu_s * m * g
```

Nếu `F_required > F_max`, script đánh dấu kiện hàng đang trượt qua biến `IsSlipping`.

## Cong thuc vat ly ap dung

Các công thức chính đang được dùng trong `ConveyorBeltDigitalTwin.cs`:

- Vận tốc góc puly: `omega = v / r`
- Lực cản ma sát gần đúng trên băng ngang: `F = mu * m * g`
- Công suất cơ học cần thiết: `P = F * v / eta`
- Mô men kéo gần đúng: `tau = F * r`
- Năng suất: `packagesPerHour = packagesPerMinute * 60`

Trong `ConveyorPackage.cs`, kiện hàng được kéo bởi lực ma sát:

- Lực cần để kéo kiện theo tốc độ băng: `F_required = m * a`
- Lực ma sát tĩnh cực đại: `F_max = mu_s * m * g`
- Nếu `F_required > F_max`, kiện bị đánh dấu là trượt.

## Cach dung thu cong trong Unity

Phần này chỉ cần dùng nếu muốn tự dựng lại từ đầu.

1. Tạo một scene mới.
2. Tạo object `ConveyorBelt`.
3. Thêm component `ConveyorBeltDigitalTwin`.
4. Tạo một cube làm mặt băng chuyền, gán vào `beltSurface` và `beltRenderer`.
5. Thêm object `TelemetrySource`, gắn component `TwinTelemetrySimulator`.
6. Kéo `TelemetrySource` vào trường `telemetrySource` của `ConveyorBeltDigitalTwin`.
7. Tạo object `HUD`, gắn `TwinMetricsHud`, rồi kéo `ConveyorBelt` vào trường `belt`.
8. Với kiện hàng, tạo cube có `Rigidbody`, `Collider`, gắn `ConveyorPackage`, rồi kéo `ConveyorBelt` vào trường `belt`.

Nếu muốn chạy như Digital Model, đổi `mode` thành `DigitalModel` và chỉnh `commandedSpeedMps`, `commandedLoadKg`.

Nếu muốn chạy như Digital Shadow, đổi `mode` thành `DigitalShadow` và dùng `TwinTelemetrySimulator`. Ngoài ra có thể dùng `CsvTelemetryPlayer`: tạo object mới, gắn script này, kéo `Assets/Data/sample-telemetry.csv` vào trường `csvFile`, rồi kéo object đó vào `csvTelemetrySource`.

