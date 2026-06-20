# Digital Twin Bang Chuyen Hang Hoa

Du an nay la khung mo phong băng chuyền hàng hóa theo hai mức:

- **Digital Model**: mô hình chạy bằng tham số nhập tay như tốc độ băng, tải trọng, khối lượng băng, ma sát.
- **Digital Shadow**: mô hình nhận dữ liệu telemetry một chiều từ cảm biến/nguồn dữ liệu. Trong bản mẫu, `TwinTelemetrySimulator` giả lập dữ liệu cảm biến theo thời gian.

Phần Unity nằm trong `UnityConveyorTwin/Assets/Scripts`.

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

## Cach dung trong Unity

1. Tạo Unity project 3D, khuyến nghị Unity 2021 LTS trở lên.
2. Copy thư mục `UnityConveyorTwin/Assets/Scripts` vào thư mục `Assets` của Unity project.
3. Tạo một scene mới.
4. Tạo object `ConveyorBelt`.
5. Thêm component `ConveyorBeltDigitalTwin`.
6. Tạo một cube làm mặt băng chuyền, gán vào `beltSurface` và `beltRenderer`.
7. Thêm object `TelemetrySource`, gắn component `TwinTelemetrySimulator`.
8. Kéo `TelemetrySource` vào trường `telemetrySource` của `ConveyorBeltDigitalTwin`.
9. Tạo object `HUD`, gắn `TwinMetricsHud`, rồi kéo `ConveyorBelt` vào trường `belt`.
10. Với kiện hàng, tạo cube có `Rigidbody`, `Collider`, gắn `ConveyorPackage`, rồi kéo `ConveyorBelt` vào trường `belt`.

Nếu muốn chạy như Digital Model, đổi `mode` thành `DigitalModel` và chỉnh `commandedSpeedMps`, `commandedLoadKg`.

Nếu muốn chạy như Digital Shadow, đổi `mode` thành `DigitalShadow` và dùng `TwinTelemetrySimulator`. Ngoài ra có thể dùng `CsvTelemetryPlayer`: tạo object mới, gắn script này, kéo `Assets/Data/sample-telemetry.csv` vào trường `csvFile`, rồi kéo object đó vào `csvTelemetrySource`.

## Goi y scene

- Băng chuyền: cube scale khoảng `(0.8, 0.12, 6)`.
- Puly: cylinder ở hai đầu băng.
- Cảm biến vào/ra: cube mỏng có `Is Trigger`, gắn `ConveyorSensor`.
- Kiện hàng: cube nhỏ scale khoảng `(0.45, 0.35, 0.45)`, có Rigidbody.

## Tieu chi cham diem co the neu

- Có phân biệt rõ **Digital Model** và **Digital Shadow**.
- Có mô phỏng trực quan trên Unity.
- Có dashboard hiển thị tốc độ, tải trọng, lực kéo, công suất, năng suất, cảnh báo.
- Có áp dụng công thức vật lý và giải thích được ý nghĩa.
- Có tình huống bất thường: quá tải, quá tốc, dừng khẩn cấp hoặc kiện bị trượt.
