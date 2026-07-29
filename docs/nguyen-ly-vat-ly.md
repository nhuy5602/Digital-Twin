# Các Nguyên lý Vật lý và Mô hình Động học trong Hệ thống Digital Twin

Tài liệu này mô tả chi tiết các nguyên lý vật lý, mô hình động học, thủy động học, va chạm không gian và logic kiểm định chất lượng được cài đặt trong mô phỏng Digital Twin của dây chuyền chiết rót, đóng nắp và phân loại chai (`Assets/Scripts/FillingFilteringDigitalTwin.cs` và `Assets/Scripts/TwinDashboardData.cs`).

---

## Danh mục 7 Quy trình Kỹ thuật

```text
1. Cấp chai & Mâm quay (Infeed Turntable & Guide Path Handoff)
2. Điều phối Scalloped Star Wheel (Pocket Indexing & Dynamic Capture)
3. Chiết rót Chất lỏng (Volumetric Liquid Filling)
4. Đóng nắp (Cap Magazine & Tightener Mechanism)
5. Kiểm tra Chất lượng (QC Sensor Beam & Quality Rules)
6. Cơ cấu Loại chai Lỗi (Reject Sweep Bar Mechanics & Escape Physics)
7. Phân làn & Đóng gói Six-Pack (A/B Splitting & Packaging Push)
```

---

## 1. Cấp chai & Mâm quay (Infeed Turntable & Guide Path Handoff)

### a. Động học Rơi Tự do (Bottle Dropper Kinematics)
Chai được cấp mới tại độ cao ban đầu phía trên mâm quay và hạ xuống bề mặt mâm thông qua quá trình rơi/nội suy tuyến tính:

$$\vec{P}(t) = \vec{P}_{\text{spawn}} + (\vec{P}_{\text{target}} - \vec{P}_{\text{spawn}}) \cdot \min\left(1, \frac{t}{T_{\text{drop}}}\right)$$

### b. Động lực học Phẳng trên Mâm quay (Rotating Turntable Physics)
Mâm quay tròn bán kính $R_{\text{turntable}} = 0.95\text{ m}$ quay với tốc độ góc $\omega$:

$$\omega = \text{RPM}_{\text{turntable}} \cdot \frac{2\pi}{60} \quad (\text{rad/s})$$

Mỗi chai trên mặt mâm chịu tác dụng của hai thành phần gia tốc chính trong mặt phẳng $XZ$:
1. **Gia tốc ly tâm (Centrifugal Acceleration)** hướng ra ngoài theo bán kính $\vec{u}_{\text{radial}}$:
   $$\vec{a}_{\text{centrifugal}} = \vec{u}_{\text{radial}} \cdot \omega^2 \cdot r$$
2. **Lực ma sát kéo bề mặt (Surface Grip Friction Acceleration)** kéo chai theo vận tốc tiếp tuyến của mâm $\vec{v}_{\text{surface}}$:
   $$\vec{v}_{\text{surface}} = \vec{u}_{\text{tangent}} \cdot (\omega \cdot r) \quad \text{với } \vec{u}_{\text{tangent}} = (-y_{\text{radial}}, x_{\text{radial}})$$
   $$\vec{a}_{\text{grip}} = \mu_{\text{grip}} \cdot (\vec{v}_{\text{surface}} - \vec{v}_{\text{bottle}})$$

Vận tốc chai được tích phân và làm chột bởi hệ số cản ma sát (velocity damping factor $d = 0.95$):

$$\vec{v}_{\text{bottle}}(t + \Delta t) = \left[ \vec{v}_{\text{bottle}}(t) + (\vec{a}_{\text{centrifugal}} + \vec{a}_{\text{grip}}) \cdot \Delta t \right] \cdot d^{\Delta t \cdot 60}$$

### c. Ràng buộc Phản lực Biên & Va chạm Phẳng (Boundary Constraints & Deflectors)
- **Biên ngoài mâm**: Nếu bán kính $r > R_{\text{max}}$, vị trí chai được chiếu ngược về $R_{\text{max}}$ và thành phần vận tốc hướng ra ngoài bị triệt tiêu:
  $$v_{\text{outward}} = \vec{v} \cdot \vec{u}_{\text{radial}} > 0 \implies \vec{v} \leftarrow \vec{v} - v_{\text{outward}} \cdot \vec{u}_{\text{radial}}$$
- **Thanh chắn chéo Deflector & Nẹp cửa ra Guide**: Phản lực nẹp được mô phỏng bằng phương pháp chiếu vận tốc triệt tiêu thành phần vuông góc với pháp tuyến mặt chắn $\hat{n}$:
  $$\vec{v} \leftarrow \vec{v} - (\vec{v} \cdot \hat{n})\hat{n}$$

### d. Giải Va chạm Phẳng 2D (Multi-body 2D Spatial Separation)
Thay vì sử dụng bộ giải vật lý 3D ngốn tài nguyên CPU của Engine, hệ thống sử dụng thuật toán giải va chạm phẳng 2D `ResolveTurntableBottleSeparation` liên tục quét khoảng cách tâm 2D giữa các chai trong mặt phẳng $XZ$.
Với bán kính chai $r_{\text{bottle}} = 0.11\text{ m}$, khoảng cách tâm an toàn giữa 2 chai là $d_{\text{min}} = 2 r_{\text{bottle}} = 0.22\text{ m}$. Khi phát hiện hai chai chồng lấn thể tích ($d < 0.22\text{ m}$):

$$\Delta \vec{P} = \frac{0.22 - d}{2} \cdot \hat{n}_{\text{separation}}$$

Mỗi chai được tự động đẩy ngược hướng một đoạn $\Delta \vec{P}$ giúp duy trì dòng chảy tự nhiên và triệt tiêu xung đột thể tích mà không sinh overhead vật lý 3D.

### e. Xấp xỉ Động học & Nội suy Chuyển tiếp Hermite (Hermite Spline & SmoothStep Kinematics)
Chai rời mâm di chuyển dọc theo chuỗi đường cong dẫn hướng (Infeed Guide Path) với vận tốc băng tải $v_{\text{conveyor}}$. 
- Tại điểm giao cắt bàn giao giữa mâm quay và guide path (`TransitioningToInfeedGuide`), hệ thống áp dụng **hàm nội suy Hermite bậc 3 (Cubic Hermite Spline)** `EvaluateCubicHermite`:
  $$\vec{P}(t) = (2t^3 - 3t^2 + 1)\vec{P}_{\text{start}} + (t^3 - 2t^2 + t)\vec{T}_{\text{start}} + (-2t^3 + 3t^2)\vec{P}_{\text{end}} + (t^3 - t^2)\vec{T}_{\text{end}}$$
  Với tiếp tuyến đầu $\vec{T}_{\text{start}} = \vec{0}$ giúp triệt tiêu vận tốc giật cục ban đầu và tiếp tuyến cuối $\vec{T}_{\text{end}}$ khớp với vận tốc băng tải.
- Các chuyển động trượt cơ khí khác (vòi rót, nắp, xi-lanh) áp dụng hàm `Mathf.SmoothStep` ($3t^2 - 2t^3$) giúp hành trình mượt mà, chống giật cục.
- Động học hàng chờ 1D đảm bảo khoảng cách tối thiểu giữa các chai: $s_{i+1}(t) \le s_i(t) - 0.22\text{ m}$.

---

## 2. Điều phối Scalloped Star Wheel (Pocket Indexing & Dynamic Capture)

### a. Động học Quay Gián đoạn & Chu kỳ Bánh sao (Intermittent Indexing Kinematics)
Đĩa Star Wheel 10 pocket quay gián đoạn để dịch chuyển chai giữa các công đoạn:
- Góc bước chia đĩa: $\Delta \theta = \frac{360^\circ}{10} = 36^\circ$ cho mỗi pocket.
- Chu kỳ luân phiên giữa **pha dừng gia công (Dwell $1.35\text{ s}$)** và **pha quay chuyển bước (Index)**.
- Vận tốc góc pha index:
  $$\omega_{\text{index}} = \text{RPM}_{\text{starwheel}} \cdot \frac{2\pi}{60}$$
- Thời gian dịch chuyển $\Delta N$ pocket (mặc định dịch chuyển 3 pocket $= 108^\circ$ cho lô 3 chai):
  $$T_{\text{index}} = \frac{\Delta N}{N_{\text{pockets}}} \cdot \frac{60}{\text{RPM}_{\text{starwheel}}}$$
  Góc quay và vị trí chai trong pha index được nội suy mượt theo thời gian thông qua `Quaternion.Slerp` và `Mathf.Lerp`.

### b. Chuyển đổi Tọa độ Cực - Cartesian (Polar-to-Cartesian Transformation)
Tọa độ pocket thứ $k$ ($k \in [0, 9]$) với góc bước chia $36^\circ$:

$$x_k(t) = x_{\text{center}} + R_{\text{wheel}} \cdot \cos\left(\theta(t) + k \cdot 36^\circ\right)$$
$$z_k(t) = z_{\text{center}} + R_{\text{wheel}} \cdot \sin\left(\theta(t) + k \cdot 36^\circ\right)$$


### c. Bắt chai vào Pocket 0 (Dynamic Pocket Capture Handoff)
Khi chai đầu hàng chờ guide path tiến vào vùng bắt ($d \le d_{\text{capture}}$), hệ thống tạo quỹ đạo chuyển tiếp dạng cung tiếp tuyến (arc interpolation) đưa chai vào đúng tâm Pocket 0 của đĩa Star Wheel.

### d. Mô hình Tách biệt Pha Dwell và Băng tải (Decoupled Dwell Mechanism)
- Thời gian đứng yên tối thiểu $T_{\text{dwell}}$ độc lập với tốc độ băng tải $v_{\text{conveyor}}$.
- Chu trình index tiếp theo chỉ kích hoạt khi **tất cả** các điều kiện sau thỏa mãn:
  1. Thời gian $t_{\text{dwell}} \ge T_{\text{dwell}}$.
  2. Cụm vòi chiết rót đã rút lên cao độ an toàn.
  3. Cụm đầu vặn nắp đã rút lên cao độ an toàn.

---

## 3. Chiết rót Chất lỏng (Volumetric Liquid Filling)

### a. Thủy động học & Định luật Bảo toàn Thể tích (Volumetric Flow Conservation)
Chiết rót sử dụng 3 vòi chiết rót song song. Lưu lượng nạp chất lỏng:
- Lưu lượng bơm tổng cộng:
  $$Q_{\text{pump}} = \frac{\text{PumpFlowLPM}}{60} \quad (\text{L/s})$$
- Lưu lượng chia đều cho $N_{\text{active}}$ chai đang ở vị trí rót:
  $$Q_{\text{nozzle}} = \frac{Q_{\text{pump}}}{N_{\text{active}}}$$
- Thể tích tích lũy trong chai sau khoảng thời gian $\Delta t$:
  $$V_{\text{liquid}}(t + \Delta t) = V_{\text{liquid}}(t) + Q_{\text{nozzle}} \cdot \Delta t$$

### b. Giới hạn Cạn bồn cấp (Vessel Depletion Limit)
Lưu lượng nạp thực tế bị khống chế bởi lượng nước khả dụng còn lại trong bồn cấp $V_{\text{vessel}}$:

$$Q_{\text{available}} = \min\left(Q_{\text{pump}}, \frac{V_{\text{vessel}}}{\Delta t}\right)$$

Khi $V_{\text{vessel}} \to 0$, lưu lượng rót về 0 và chai rơi vào trạng thái thiếu nước (Underfill).

### c. Mô hình Trào nước Không va chạm (Visual Overflow Physics)
Dung tích danh định của chai là $V_{\text{nominal}} = 1.0\text{ L}$.
- Tỷ lệ đày nước:
  $$\alpha = \frac{V_{\text{liquid}}}{V_{\text{nominal}}}$$
- Khi $\alpha > 1.05$ (vượt 105%), hiện tượng trào nước được kích hoạt:
  - Sinh ra 2 vỏ lớp nước bề mặt (`Overflow Water - Body` và `Overflow Water - Neck`) bao quanh bên ngoài thân và cổ chai.
  - Các lớp vỏ nước này hiển thị vật liệu hiệu ứng nước sáng nhưng **tắt collider** để không làm biến dạng động học truyền động hay va chạm cơ khí.

### d. Động học Hành trình Vòi chiết (Nozzle Actuation Kinematics)
Cụm vòi rót di chuyển dọc theo trục thẳng đứng $Y$:
1. Lowering stroke: $Y_{\text{ready}} \to Y_{\text{fill}}$ trong thời gian $t_{\text{stroke}}$.
2. Filling dwell: Giữ nguyên $Y_{\text{fill}}$ và mở van bơm trong $T_{\text{dwell}}$.
3. Raising stroke: $Y_{\text{fill}} \to Y_{\text{ready}}$ chuẩn bị cho đĩa quay index.

---

## 4. Đóng nắp (Cap Magazine & Tightener Mechanism)

### a. Động học Cột nắp Trượt Tự do (Cap Magazine Gravity Feed)
Cột cấp nắp chứa hàng nắp xếp đứng với bước nắp $p_{\text{cap}} = 0.11\text{ m}$. Khi một nắp ở đáy được cấp cho chai:
- Các nắp phía trên trượt xuống theo động học rơi có lực cản:
  $$Y_i(t + \Delta t) = Y_i(t) - v_{\text{slide}} \cdot \Delta t$$

### b. Động học Đầu Vặn nắp (Tightener Head Kinematics & Torque Simulation)
Cụm vặn nắp thực hiện chu trình 3 bước:
1. **Hạ đầu vặn**: Di chuyển thẳng đứng xuống vị trí cổ chai $Y_{\text{cap}}$.
2. **Tác dụng xoắn (Torque Application)**: Xoay đầu vặn với tốc độ góc $\omega_{\text{tighten}}$ để siết ren nắp vào cổ chai.
3. **Rút đầu vặn**: Trở về độ cao ban đầu trước khi Star Wheel index.

---

## 5. Kiểm tra Chất lượng (QC Sensor Beam & Quality Rules)

### a. Cảm biến Quang học Cắt Tia (Optical Sensor Beam Triggers)
Cảm biến QC đặt tại tọa độ băng tải $Z_{\text{QC}} = 0.85\text{ m}$. Khi tọa độ chai $Z_{\text{bottle}} \ge Z_{\text{QC}}$ và chai chưa kiểm định:

$$\text{Trigger inspection event at } t_{\text{inspect}}$$

### b. Quy tắc Kiểm định Chất lượng Thống kê (SQC Rules)
Kết quả phân loại chất lượng dựa trên tỷ lệ đày nước $\alpha = \frac{V_{\text{liquid}}}{V_{\text{nominal}}}$:

$$\text{Status} = \begin{cases} \text{Passed} & \text{nếu } 0.95 \le \alpha \le 1.05 \\ \text{Rejected (Underfill)} & \text{nếu } \alpha < 0.95 \\ \text{Rejected (Overflow)} & \text{nếu } \alpha > 1.05 \end{cases}$$

---

## 6. Cơ cấu Loại chai Lỗi (Reject Sweep Bar Mechanics & Escape Physics)

### a. Động học Quét Ngang (Transverse Sweeping Kinematics)
Thanh gạt lỗi đặt tại $Z_{\text{station}} = 2.25\text{ m}$. Khi có chai lỗi tiến vào vùng quét:
- Thanh gạt thực hiện hành trình tiến ra $X_{\text{rest}} \to X_{\text{max}}$ và lùi về $X_{\text{max}} \to X_{\text{rest}}$.

### b. Phát hiện Va chạm Hình học 3D/2D (Continuous Sweep Bounds Collision)
Tại mỗi frame của hành trình quét, hệ thống tính khoảng cách giữa vị trí tâm chai $\vec{P}_{\text{bottle}}$ và hình hộp giới hạn $\text{AABB}_{\text{sweep}}$ của thanh gạt:

$$d_{\text{horizontal}}(\vec{P}_{\text{bottle}}, \text{AABB}_{\text{sweep}}) = \left| \vec{P}_{\text{bottle}} - \text{ClosestPoint}(\text{AABB}_{\text{sweep}}, \vec{P}_{\text{bottle}}) \right|_{XZ}$$

Nếu $d_{\text{horizontal}} \le r_{\text{bottle}}$, chai va chạm với thanh gạt và bị gạt khỏi băng tải vào khay loại (`Rejected Bottle Tray`).

### c. Hiện tượng Chai lỗi Trốn gạt (Reject Escape Physics)
Chai di chuyển liên tục theo chiều dọc băng tải $Z(t) = Z_0 + v_{\text{conveyor}} \cdot t$.
- Nếu tốc độ băng tải $v_{\text{conveyor}}$ quá lớn so với vận tốc quét ngang của thanh gạt, chai vượt qua khỏi chiều dài vùng quét trước khi thanh gạt đến vị trí chặn:
  $$Z_{\text{bottle}} > Z_{\text{station}} + L_{\text{sweep}} + r_{\text{bottle}}$$
- Chai lỗi trốn gạt sẽ không bị loại, tiếp tục đi xuống làn đóng gói như chai thường và được ghi nhận vào chỉ số **Reject Escapes**.

---

## 7. Phân làn & Đóng gói Six-Pack (A/B Splitting & Packaging Push)

### a. Động học Thanh Định hướng & Khoảng Thời gian An toàn (Diverter Guide Kinematics & Clearance Time)
Thanh phân làn xoay quanh tâm $Z_{\text{pivot}}$ để chuyển hướng luồng chai giữa Lane A ($0^\circ$) và Lane B ($\theta_{\text{split}}$).
- Khoảng thời gian an toàn cho phép thanh gạt xoay chuyển làn $t_{\text{available}}$ để không va chạm với chai tiếp theo:
  $$t_{\text{available}} = \frac{d_{\text{spacing}} - 2 r_{\text{bottle}} - d_{\text{gap}}}{v_{\text{conveyor}}}$$

### b. Bộ đếm Chai Phân làn A/B (Photo-Eye Batch Counter)
Cảm biến hồng ngoại đếm số chai đạt chuẩn đi qua. Khi số đếm ở làn hiện tại đạt $N_{\text{lane}} = 3$ chai:
- Kích hoạt tín hiệu xoay thanh định hướng chuyển luồng chai sang làn đối diện để hình thành khối 6 chai cân bằng $50/50$.

### c. Hình học Tọa độ Ma trận 3x2 (3x2 Grid Slot Positioning)
Trạm đóng gói hình thành lô 6 chai dạng **Ma trận 3x2** (3 hàng dọc $\times$ 2 cột làn A/B):
- Tọa độ 3D $(X_i, Z_i)$ của chai thứ $i$ ($i \in [0, 5]$):
  $$Z_i = Z_{\text{hàng đầu}} - (\text{row}) \cdot p_{\text{hàng}} \quad \text{với } \text{row} = i \bmod 3$$
  $$X_i = X_{\text{thùng}} \pm \frac{\Delta X_{\text{làn}}}{2}$$
- Trong đó bước khoảng cách hàng $p_{\text{hàng}} = \max(p_{\text{danh định}}, \; 2 r_{\text{bottle}} + 0.015\text{m})$ khống chế chai không bị va nén vỡ.

### d. Động lực học Chống đổ Chai bằng Gia tốc Mượt (SmoothStep Pusher Dynamics)
Xi-lanh đẩy `PackPusher` tiến ngang đẩy cả khối 6 chai (dạng ma trận 3x2) vào thùng carton six-pack:
- Quỹ đạo di chuyển $X(t)$ điều khiển theo hàm SmoothStep $S(t)$:
  $$S(t) = 3t^2 - 2t^3 \quad \left(t = \frac{t_{\text{elapsed}}}{T_{\text{pusher}}} \in [0, 1]\right)$$
  $$X(t) = X_{\text{start}} + (X_{\text{end}} - X_{\text{start}}) \cdot S(t)$$
- Vận tốc tức thời: $v(t) = \frac{dX}{dt} = 6t(1 - t) \cdot \frac{\Delta X}{T_{\text{pusher}}}$.
  - Tại $t = 0 \implies v(0) = 0$ (khởi động êm từ 0, không bị giật ban đầu).
  - Tại $t = 1 \implies v(1) = 0$ (chậm dần về 0 trước khi dừng, không bị nảy va đập).
- **Lực quán tính $F = m \cdot a(t)$** biến thiên mượt mà giúp 6 chai đứng vững $100\%$ không bị ngã đổ.
- Sau khi đẩy xong, xi-lanh lùi về vị trí ban đầu và mở cửa chặn `PackStopGate` cho lô chai tiếp theo.



---

## Bảng Tóm tắt các Thông số Kỹ thuật & Công thức

| Tham số / Quy trình | Giá trị / Công thức mặc định | Ý nghĩa vật lý / Tác động |
| --- | --- | --- |
| Tốc độ băng tải $v_{\text{conveyor}}$ | $0.85\text{ m/s}$ (0.20–2.50 m/s) | Quyết định tốc độ chuyển chai, ảnh hưởng đến tỷ lệ trốn gạt Reject Escape. |
| Vận tốc mâm quay $\text{RPM}_{\text{turntable}}$ | $18\text{ rpm}$ | Quyết định lực ly tâm $\omega^2 r$ dạt chai ra biên cửa xả. |
| Tốc độ đĩa Star Wheel $\text{RPM}_{\text{starwheel}}$ | $6.67\text{ rpm}$ | Tốc độ quay góc trong pha index giữa các station. |
| Thời gian đứng yên $T_{\text{dwell}}$ | $1.35\text{ s}$ | Cửa sổ thời gian rót nước và đóng nắp tối thiểu. |
| Lưu lượng bơm $Q_{\text{pump}}$ | $133.33\text{ L/min}$ ($2.222\text{ L/s}$) | Lưu lượng nạp chất lỏng tổng cho 3 vòi chiết rót. |
| Dung tích danh định chai $V_{\text{nominal}}$ | $1.00\text{ L}$ | Chuẩn dung tích 100%. |
| Ngưỡng Đạt chuẩn Pass | $0.95 \le \alpha \le 1.05$ | Khống chế sai số thể tích trong khoảng $\pm 5\%$. |
| Ngưỡng Overflow | $\alpha > 1.05$ | Trào nước ra ngoài vỏ chai và bị đánh dấu Reject. |
