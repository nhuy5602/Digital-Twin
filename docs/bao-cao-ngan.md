# Báo cáo Tổng quan Kỹ thuật: Digital Twin Dây chuyền Chiết rót, Đóng nắp & Phân loại Quality Line

Tài liệu này cung cấp báo cáo tổng quan chi tiết về hệ thống mô phỏng Digital Twin (Digital Model) cho dây chuyền sản xuất chai thủy tinh tự động: cấp chai bằng mâm quay, dẫn chai bằng băng tải nẹp, điều phối gián đoạn bằng đĩa Star Wheel 10 pocket, chiết rót 3 vòi song song, đóng nắp, kiểm tra chất lượng QC quang học, loại chai lỗi bằng thanh gạt ngang, phân 2 làn A/B và đóng gói thùng carton six-pack.

---

## 1. Mục tiêu & Phạm vi Mô phỏng

### a. Mục tiêu Dự án
- **Mô phỏng 3D Thời gian thực**: Xây dựng mô hình Digital Twin trên Unity (khuyến nghị phiên bản `Unity 6000.5.0f1`) mô phỏng trực quan chuyển động vật lý, dòng di chuyển vật liệu và chu trình cơ khí chính xác của dây chuyền đóng chai.
- **Kiểm thử Logic Vận hành & Thuật toán Toán học**: Kiểm chứng sự tác động của các tham số vận hành (Setpoint) như tốc độ băng tải, lưu lượng bơm, tốc độ mâm quay, tốc độ góc và thời gian dwell của đĩa Star Wheel tới chất lượng chiết rót và năng suất line.
- **Phân tích Hiện tượng Chất lượng & Lỗi sản xuất**: Đánh giá các hiện tượng rót thiếu (Underfill), rót tràn (Overflow), trốn gạt lỗi (Reject Escape) và va chạm ngoài ý muốn khi thay đổi tốc độ line.

### b. Giới hạn Mô hình
- Đây là **Digital Twin mô phỏng độc lập (Standalone Simulation)**. Hệ thống hiển thị HUD và nhận điều khiển cục bộ trên máy tính; chưa kết nối trực tiếp với PLC thực hoặc hệ thống SCADA/IIoT nhà máy.
- Mô hình sử dụng các công thức động học, thủy động học tích phân và kiểm tra va chạm hình học (AABB / Bounding box) để đạt hiệu năng 60 FPS mượt mà, không giải va chạm vật lý Rigidbody/CFD toàn phần.

---

## 2. Luồng Công nghệ & 7 Quy trình Sản xuất

Sơ đồ tổng quát dòng di chuyển của chai trên dây chuyền:

```text
[1. Bottle Dropper]
        │ (Rơi tự do)
        ▼
[2. Infeed Turntable] ──► [Outlet Transfer Plate] ──► [Guide Rail Path]
                                                             │
                                                             ▼
                                                [3. Scalloped Star Wheel]
                                                ┌────────────┴────────────┐
                                                │ (Pocket 0 Handoff)      │
                                                │ • Index 3 pockets (108°)│
                                                │ • [4. 3-Nozzle Fill]    │
                                                │ • [5. 3-Head Capper]    │
                                                └────────────┬────────────┘
                                                             │ (Xả ra Băng tải)
                                                             ▼
                                                    [6. QC Sensor Beam]
                                                             │
                                        ┌────────────────────┴────────────────────┐
                                        ▼                                         ▼
                            [7. Reject Sweep Bar]                     [8. A/B Lane Splitter]
                                        │                                         │
                                        ▼                                         ▼
                             [Rejected Bottle Tray]                     [Six-Pack Carton Pusher]
```

### Chi tiết 7 Quy trình chính:
1. **Cấp chai & Mâm quay (Infeed Turntable)**: Chai từ trạm rơi xuống mâm quay tròn bán kính $0.95\text{ m}$. Lực ly tâm và lực ma sát bề mặt dạt chai ra phía biên. Thanh gạt chéo (Diagonal Deflector) và tấm trượt bàn giao (Transfer Plate) định hướng chai tiến vào đường nẹp dẫn hướng (Guide Rail Path).
2. **Điều phối Scalloped Star Wheel**: Đĩa Star Wheel 10 pocket đón chai tại Pocket 0. Mỗi chu trình đĩa index $108^\circ$ (dịch chuyển 3 pocket), đưa 3 chai đồng thời vào cụm trạm rót và đóng nắp.
3. **Chiết rót Chất lỏng (Volumetric Filling)**: Cụm 3 vòi rót song song hạ xuống miệng chai, rót nước từ bồn chứa trong khoảng thời gian Dwell ($T_{\text{dwell}}$).
4. **Đóng nắp (Cap Magazine & Tightener)**: Hàng nắp tự chảy trong ống nghiêng/đứng cấp nắp cho chai; cụm 3 đầu vặn nắp hạ xuống, tác dụng mô-men xoắn siết chặt nắp vào cổ chai.
5. **Kiểm tra Chất lượng (QC Sensor Beam)**: Cảm biến quang học tại $Z_{\text{QC}} = 0.85\text{ m}$ quét dung tích thực trong chai và phân loại Đạt (Pass: $95\% - 105\%$) hoặc Lỗi (Underfill $<95\%$, Overflow $>105\%$).
6. **Loại chai Lỗi (Reject Sweep Bar)**: Thanh gạt ngang tại $Z_{\text{station}} = 2.25\text{ m}$ quét ra để gạt các chai lỗi vào khay `Rejected Bottle Tray`. Chai chạy quá nhanh trên băng tải có thể trốn gạt (Reject Escape).
7. **Phân làn & Đóng gói Six-Pack (A/B Splitting & Packaging)**: Thanh phân làn xoay chuyển hướng luồng chai giữa Lane A và Lane B khi bộ đếm hồng ngoại đếm đủ 6 chai đạt chuẩn; xi-lanh đẩy ngang đưa cả lô 6 chai vào thùng carton six-pack.

---

## 3. Bộ Setpoint Vận hành & Presets Thử nghiệm

### a. Bộ Setpoint Vận hành Cốt lõi

| Setpoint | Phạm vi Cấu hình | Mặc định | Tác động Vận hành & Hệ quả |
| --- | ---: | ---: | --- |
| **Conveyor Speed** | $0.20 - 2.50\text{ m/s}$ | $0.85\text{ m/s}$ | Quyết định tốc độ di chuyển chai ngoài Star Wheel. Tốc độ quá cao làm tăng tỷ lệ chai lỗi trốn gạt (Reject Escape) hoặc gạt nhầm chai pass. Không làm thay đổi dwell rót. |
| **Pump Flow** | $0 - 300\text{ L/min}$ | $133.33\text{ L/min}$ | Lượng nước bơm vào chai trong thời gian dwell ($133.33\text{ L/min} \rightarrow 1.00\text{ L}$ danh định trong $1.35\text{ s}$). Lưu lượng $<126.67\text{ L/min}$ gây Underfill, $>140.00\text{ L/min}$ gây Overflow. |
| **Infeed Turntable RPM** | $5 - 60\text{ rpm}$ | $18.0\text{ rpm}$ | Tốc độ quay của mâm cấp chai. Quyết định gia tốc ly tâm $\omega^2 r$ dạt chai về cửa ra và nhịp xả chai vào guide path. |
| **Star Wheel Index Speed** | $1 - 30\text{ rpm}$ | $6.67\text{ rpm}$ | Tốc độ góc của đĩa Star Wheel trong pha index. Quyết định thời gian di chuyển giữa các station ($T_{\text{index}} \approx 0.90\text{ s}$ ở $6.67\text{ rpm}$). |
| **Star Wheel Dwell** | $0.10 - 5.00\text{ s}$ | $1.35\text{ s}$ | Thời gian đĩa đứng yên tối thiểu tại các station. Đây là cửa sổ thời gian rót nước và vặn nắp. |

### b. Các Scenario Presets Thử nghiệm Cài sẵn

| Preset | Cấu hình Setpoints | Mục tiêu Quan sát Kỹ thuật |
| --- | --- | --- |
| **Nominal** | Bộ setpoint mặc định ($v=0.85$, $Q=133.33$, $\text{Turntable}=18$, $\text{Index}=6.67$, $\text{Dwell}=1.35$) | Chu trình vận hành tiêu chuẩn, đạt 100% dung tích danh định (1.00 L), tỷ lệ Pass 100%. |
| **High Conveyor** | Tăng Conveyor speed lên $1.40\text{ m/s}$ ($1.65\times$) | Thử nghiệm khả năng đánh chặn của Reject Sweep Bar; quan sát hiện tượng Reject Escape và va chạm chai ngoài Star Wheel. |
| **Low Pump Flow** | Giảm Pump flow xuống $73.33\text{ L/min}$ ($55\%$) | Tạo lỗi rót thiếu nước (Underfill $\approx 0.55\text{ L}$), quan sát cảm biến QC phát hiện và thanh gạt loại chai lỗi. |
| **High Infeed RPM** | Tăng Infeed Turntable lên $29.70\text{ rpm}$ ($1.65\times$) | Tăng lực ly tâm đẩy chai trên mâm quay, kiểm tra khả năng tích trữ của mâm và chống kẹt nẹp cửa ra. |
| **Fast Disc Index** | Disc Index Speed = $30.0\text{ rpm}$ | Rút ngắn thời gian di chuyển index giữa các trạm ($T_{\text{index}} \approx 0.20\text{ s}$), tăng năng suất chu trình. |
| **Slow Disc Index** | Disc Index Speed = $2.0\text{ rpm}$ | Kéo dài thời gian di chuyển index ($T_{\text{index}} \approx 3.0\text{ s}$), quan sát chính xác hành trình hạ/nâng của vòi rót. |
| **Short Disc Dwell** | Disc Dwell = $0.35\text{ s}$ | Cửa sổ rót nước cực ngắn gây thiếu nước nghiêm trọng (Underfill), kiểm tra logic khóa index an toàn. |
| **Long Disc Dwell** | Disc Dwell = $3.50\text{ s}$ | Kéo dài chu trình dwell, giảm tổng năng suất chai/giờ. |
| **Overflow Pump Test**| Pump Flow = $300.0\text{ L/min}$ ($2.25\times$), Dwell = $1.35\text{ s}$ | Tạo lượng nước rót $2.25\text{ L/chai}$ ($225\%$), kích hoạt lớp hiệu ứng nước trào `Overflow Water Shell` bao quanh thân và cổ chai. |

---

## 4. Tóm tắt Mô hình Vật lý & Logic Chất lượng

1. **Động cơ & Động lực học Mâm quay**: Vận tốc góc $\omega = \text{RPM} \cdot \frac{2\pi}{60}$. Lực ly tâm $\vec{a}_{\text{centrifugal}} = \vec{u}_{\text{radial}} \cdot \omega^2 r$ kết hợp ma sát kéo bề mặt $\vec{a}_{\text{grip}} = \mu (\vec{v}_{\text{surface}} - \vec{v}_{\text{bottle}})$ đẩy chai dạt ra biên. Va chạm nẹp đệm được giải bằng chiếu vectơ vận tốc triệt tiêu thành phần vuông góc pháp tuyến $\hat{n}$.
2. **Chiết rót & Bảo toàn Thể tích**: Lưu lượng nạp mỗi chai $Q_{\text{nozzle}} = Q_{\text{pump}} / N_{\text{active}}$. Thể tích chất lỏng $V_{\text{liquid}}(t+\Delta t) = V_{\text{liquid}}(t) + Q_{\text{nozzle}} \Delta t$. Lượng bơm thực tế bị bounded bởi thể tích khả dụng còn lại trong bồn chứa $V_{\text{vessel}}$.
3. **Hiện tượng Trào nước (Overflow)**: Khi $V_{\text{liquid}} > 1.05\text{ L}$, chai bật trạng thái `isOverflowed`. 2 lớp vỏ nước `Overflow Water - Body` và `Overflow Water - Neck` hiển thị vật liệu nước xanh sáng bao ngoài chai nhưng **tắt collider** để không cản trở hành trình cơ khí của cụm vặn nắp Tightener.
4. **Tiêu chuẩn QC & Thanh gạt Loại chai Lỗi**:
   - Ngưỡng Pass: $95\% \le \text{Fill Ratio} \le 105\%$ ($0.95\text{ L} \le V \le 1.05\text{ L}$).
   - Reject Sweep Bar kiểm tra giao điểm va chạm AABB hình hộp thanh gạt với bán kính chai $r_{\text{bottle}}$ liên tục trong từng frame hành trình quét.
   - Nếu $Z_{\text{bottle}} > Z_{\text{station}} + L_{\text{sweep}} + r_{\text{bottle}}$ trước khi thanh gạt chặn ngang, chai sẽ bị trốn gạt (Reject Escape).

---

## 5. Hệ thống KPI & Cảnh báo Sự cố (Alerts)

Mô hình tính toán và đẩy dữ liệu snapshot `TwinSnapshot` lên Unity HUD và Web Dashboard theo thời gian thực:

### a. Các Chỉ số KPI Quan trọng
- **Throughput (Bottles/Hour)**: Tổng Năng suất quy đổi theo giờ.
- **Recent Good Output**: Năng suất chai đạt chuẩn trong cửa sổ 60 giây gần nhất.
- **Average Fill % & Last Batch Fill %**: Tỷ lệ đày nước trung bình toàn bộ và của lô 3 chai vừa rót.
- **QC Pass Rate % & Reject Rate %**: Tỷ lệ chai đạt chuẩn QC và tỷ lệ chai bị loại.
- **Overflow Rate % & Reject Escape Rate %**: Tỷ lệ chai bị trào nước và tỷ lệ chai lỗi thoát qua thanh gạt.
- **Vessel Level (Liters)**: Mức nước còn lại trong bồn cấp ($0 - 150\text{ L}$).
- **Star Wheel Phase**: Pha hoạt động hiện tại (`Dwell/Filling`, `Indexing`, `Idle`).

### b. Hệ thống Cảnh báo Sự cố (Alert System)
- `Overflow detected`: Phát hiện chai có lượng nước $>105\%$.
- `Underfill detected`: Phát hiện chai có lượng nước $<95\%$.
- `Low vessel level`: Mức bồn nước còn lại dưới $15\%$.
- `Reject escape detected`: Phát hiện chai lỗi lọt qua thanh gạt Sweep Bar.
- `High reject rate`: Tỷ lệ reject vượt quá ngưỡng cảnh báo $10\%$.
- `Turntable buffer near full`: Số chai nằm trên mâm vượt quá $80\%$ sức chứa buffer.

---

## 6. Kiến trúc Phần mềm & Mã nguồn Dự án

| Tệp Mã nguồn | Vai trò & Trách nhiệm trong Kiến trúc |
| --- | --- |
| [Assets/Scripts/ConveyorDemoRuntimeBootstrap.cs](file:///d:/work/personal_work/digital-twin/Assets/Scripts/ConveyorDemoRuntimeBootstrap.cs) | Dựng toàn bộ hình học 3D, mesh trạm, mâm quay, đường dẫn path, vòi rót, nắp và HUD khi khởi chạy. |
| [Assets/Scripts/FillingFilteringDigitalTwin.cs](file:///d:/work/personal_work/digital-twin/Assets/Scripts/FillingFilteringDigitalTwin.cs) | Core Engine quản lý trạng thái, tích phân động học mâm quay, coroutine index/fill/cap, QC beam, reject sweeper và A/B splitter. |
| [Assets/Scripts/BottleProcessState.cs](file:///d:/work/personal_work/digital-twin/Assets/Scripts/BottleProcessState.cs) | Entity quản lý trạng thái từng chai (thể tích $V_{\text{liquid}}$, màu sắc đại diện chất lượng, nắp chai và hiệu ứng vỏ water overflow). |
| [Assets/Scripts/TwinDashboardData.cs](file:///d:/work/personal_work/digital-twin/Assets/Scripts/TwinDashboardData.cs) | Chứa các data contract (`TwinSnapshot`, `TwinSetpoints`), enum Presets và các hàm toán thuần testable (`TwinProcessMath`). |
| [Assets/Scripts/FillingFilteringHud.cs](file:///d:/work/personal_work/digital-twin/Assets/Scripts/FillingFilteringHud.cs) | Giao diện điều khiển HUD trực quan trên Unity Screen Canvas, slider tương tác thời gian thực. |
| [Assets/Editor/ConveyorDemoSceneBuilder.cs](file:///d:/work/personal_work/digital-twin/Assets/Editor/ConveyorDemoSceneBuilder.cs) | Menu công cụ Editor (`Tools > Conveyor Twin > Build Demo Scene`) dựng lại scene tự động. |
| [Assets/Editor/TwinProcessMathTests.cs](file:///d:/work/personal_work/digital-twin/Assets/Editor/TwinProcessMathTests.cs) | Bộ kiểm thử tự động EditMode kiểm tra tính đúng đắn của các công thức toán học và logic kiểm định QC. |

---

## 7. Hướng dẫn Dựng Scene & Tinh chỉnh trong Unity

### a. Cách Chạy Demo Nhanh
1. Mở Unity Editor với phiên bản **Unity 6000.5.0f1**.
2. Mở Scene: `Assets/Scenes/SampleScene.unity`.
3. Bấm **Play**.
4. Bảng điều khiển HUD xuất hiện ở góc trên bên trái. Kéo các thanh slider để điều chỉnh tốc độ line trực tiếp.
5. Muốn nạp lại nước bồn chứa hoặc đổi seed ngẫu nhiên, bấm nút **Reset** hoặc **New seed + reset** trên HUD.

### b. Tinh chỉnh Hình học trong Inspector
Chọn GameObject `Filling Filtering Demo Bootstrap` trong Inspector để tinh chỉnh:
- **Bottle height scale**: Điều chỉnh chiều cao vỏ chai ($0.80 - 1.00$).
- **Infeed tail curve tuning**: Tinh chỉnh góc cong của cặp rail dẫn chai nhập vào Star Wheel.
- **Star wheel continuous barrier tuning**: Điều chỉnh độ mở góc rào chắn bảo vệ quanh đĩa Star Wheel.
- Gọi menu **Tools > Conveyor Twin > Build Demo Scene** hoặc bấm **Rebuild Filling & Filtering Demo** để lưu lại scene sinh tự động.

---

## 8. Liên kết Tài liệu Kỹ thuật Chi tiết

Để nghiên cứu sâu hơn về cơ sở lý thuyết toán học và sơ đồ kiến trúc hệ thống, vui lòng tham khảo các tài liệu chuyên sâu:

- 📑 [Các Nguyên lý Vật lý theo Quy trình](nguyen-ly-vat-ly.md): Phân tích công thức động học mâm quay, lực ly tâm, ma sát kéo bề mặt, toán học chiết rót, trào nước và bài toán va chạm thanh gạt lỗi.
- 📐 [Sơ đồ Thiết kế Hệ thống (C4 Model)](so-do-thiet-ke-he-thong.md): Kiến trúc phần mềm chuẩn C4 (Context, Container, Component, Code/Class) với sơ đồ Mermaid trực quan.
- ⚙️ [Geometry & Handoff Logic](scalloped-star-wheel-logic.md): Ghi chú chi tiết về hình học đĩa Star Wheel 10 pocket và bài toán handoff từ mâm quay.
