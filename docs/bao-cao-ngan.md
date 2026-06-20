# Bao cao ngan: Digital Twin bang chuyen hang hoa

## 1. Muc tieu

Xây dựng mô hình digital twin cho hệ thống băng chuyền hàng hóa nhằm mô phỏng chuyển động, theo dõi trạng thái vận hành và cảnh báo khi có hiện tượng quá tải hoặc quá tốc.

## 2. Muc do digital twin

### Digital Model

Ở mức Digital Model, mô hình Unity hoạt động bằng các tham số cấu hình thủ công:

- tốc độ băng chuyền,
- tải trọng hàng hóa,
- khối lượng băng,
- bán kính puly,
- hệ số ma sát,
- hiệu suất động cơ.

Dữ liệu không tự động đồng bộ từ hệ thống thật.

### Digital Shadow

Ở mức Digital Shadow, mô hình nhận dữ liệu một chiều từ nguồn telemetry. Trong bản mẫu, script `TwinTelemetrySimulator` giả lập dữ liệu cảm biến gồm:

- tốc độ băng,
- tải trọng,
- số kiện/phút,
- mô men động cơ,
- công suất đo được,
- trạng thái dừng khẩn cấp.

Khi có dữ liệu thật, nguồn giả lập có thể thay bằng dữ liệu từ PLC, MQTT, OPC UA, REST API hoặc file CSV.

## 3. Thanh phan he thong

- `ConveyorBeltDigitalTwin`: lõi mô phỏng băng chuyền và tính toán vật lý.
- `TwinTelemetrySimulator`: giả lập dòng dữ liệu cảm biến cho chế độ Digital Shadow.
- `ConveyorPackage`: mô phỏng kiện hàng chịu lực ma sát từ băng.
- `ConveyorSensor`: đếm kiện hàng đi qua vùng cảm biến.
- `TwinMetricsHud`: hiển thị thông số vận hành trong Unity.

## 4. Cong thuc vat ly

### Van toc goc puly

```text
omega = v / r
```

Trong đó:

- `omega`: vận tốc góc của puly, đơn vị rad/s,
- `v`: vận tốc dài của băng, đơn vị m/s,
- `r`: bán kính puly, đơn vị m.

### Luc can ma sat

```text
F = mu * m * g
```

Trong đó:

- `F`: lực kéo cần để thắng ma sát, đơn vị N,
- `mu`: hệ số ma sát lăn/gần đúng,
- `m`: tổng khối lượng chuyển động gồm băng và hàng, đơn vị kg,
- `g`: gia tốc trọng trường, xấp xỉ 9.81 m/s2.

### Cong suat dong co

```text
P = F * v / eta
```

Trong đó:

- `P`: công suất cần thiết, đơn vị W,
- `F`: lực kéo, đơn vị N,
- `v`: tốc độ băng, đơn vị m/s,
- `eta`: hiệu suất động cơ.

### Mo men keo

```text
tau = F * r
```

Trong đó:

- `tau`: mô men tại puly, đơn vị N.m,
- `F`: lực kéo, đơn vị N,
- `r`: bán kính puly, đơn vị m.

### Nang suat van chuyen

```text
Q = packagesPerMinute * 60
```

`Q` là số kiện hàng mỗi giờ.

## 5. Canh bao

Mô hình sinh cảnh báo khi:

- tải trọng hiện tại lớn hơn tải trọng an toàn,
- tốc độ băng lớn hơn tốc độ an toàn,
- kiện hàng cần lực kéo lớn hơn lực ma sát tĩnh cực đại nên có nguy cơ trượt.

## 6. Huong phat trien

- Kết nối dữ liệu thật từ PLC hoặc cảm biến cân tải.
- Thêm thuật toán dự báo quá tải.
- Lưu lịch sử vận hành vào CSV/database.
- Thêm biểu đồ thời gian thực.
- Điều khiển ngược về hệ thống thật để nâng cấp từ Digital Shadow lên Digital Twin đầy đủ.

