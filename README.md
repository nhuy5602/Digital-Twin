# Digital Twin — Filling, Capping & Quality Line

Mô phỏng Unity cho dây chuyền chai: cấp chai từ mâm xoay, đưa vào Scalloped Star Wheel Disc, rót nước, đóng nắp, kiểm tra chất lượng, loại chai lỗi và đóng six-pack.

Đây là **digital twin mô phỏng**. HUD chỉ điều khiển trạng thái mô phỏng trên máy cục bộ; không có kết nối PLC, thiết bị thật hoặc telemetry sản xuất.

## Chạy nhanh

Yêu cầu: **Unity 6000.5.0f1**.

1. Mở [Assets/Scenes/SampleScene.unity](Assets/Scenes/SampleScene.unity).
2. Bấm Play.
3. HUD xuất hiện ở góc trên trái. Có thể chỉnh slider trực tiếp; giá trị được áp dụng ngay khi kéo.
4. Để dựng lại toàn bộ phần scene sinh tự động, dùng **Tools > Conveyor Twin > Build Demo Scene**.

## Luồng công nghệ

    Bottle Dropper
      -> Infeed Turntable + buffer
      -> Transfer plate + guide rails
      -> 10-pocket Scalloped Star Wheel Disc
         -> index -> dwell/fill -> cap -> index
      -> QC sensor beam
      -> Reject Sweep Bar -> reject tray
         hoặc -> A/B splitter -> six-pack carton

Star Wheel là điểm điều phối chính:

- Disc có 10 pocket và index 3 pocket cho mỗi bước làm việc.
- Cụm rót có 3 nozzle; chỉ rót khi Disc đã dừng và các filling pocket có chai.
- Tightener chỉ hạ/xoắn/nâng khi Disc dừng, tại các capping pocket có chai.
- Sau khi dwell tối thiểu hoàn tất và Tightener đã về trạng thái an toàn, Disc mới index tiếp.

Conveyor vẫn chạy để vận chuyển chai ngoài Star Wheel. Khi Disc đang rót hoặc đóng nắp, chai trong pocket được giữ tại station; tốc độ conveyor không rút ngắn dwell rót.

## Các setpoint vận hành

| Setpoint | Phạm vi | Mặc định | Tác động chính |
| --- | ---: | ---: | --- |
| Conveyor | 0.20–2.50 m/s | 0.85 m/s | Chuyển chai, năng suất và khả năng chai lỗi lọt qua Reject Sweep Bar. Không làm đổi dwell rót. |
| Pump flow | 0–300 L/min | 133.33 L/min | Lượng nước cấp trong dwell; quá thấp gây thiếu nước, quá cao gây trào. |
| Infeed turntable | 5–60 rpm | 18 rpm | Nhịp giải phóng chai từ buffer vào guide; có giới hạn bởi khoảng cách an toàn trên line. |
| Disc index speed | 1–30 rpm | 6.67 rpm | Tốc độ quay của Disc trong pha index và thời gian index giữa các station. |
| Disc dwell | 0.10–5.00 s | 1.35 s | Thời gian Disc đứng yên tối thiểu ở station; đây là cửa sổ rót nước. |

Thời gian index được tính theo số pocket cần đi:

    index duration = (số pocket dịch chuyển / tổng số pocket) × 60 / Disc RPM

Ví dụ: index 1 pocket ở 6.67 rpm trên Disc 10 pocket mất xấp xỉ 0.90 s. Dwell là thông số độc lập: thay Conveyor không làm thay đổi Disc dwell.

## Mô hình rót nước và QC

Mỗi chai có dung tích danh định 1 L. Bồn cấp nước đang ở chế độ vô hạn, nên mức bồn luôn đầy và không giới hạn lưu lượng bơm. Trong toàn bộ dwell:

1. Van/nozzle tiếp tục mở với mọi chai đang ở filling pocket, kể cả chai đã đạt 100%.
2. Lưu lượng bơm khả dụng được giới hạn bởi lượng còn lại trong bồn.
3. Lưu lượng được chia đều giữa các chai đang rót.
4. Lượng nước thực tế được giữ nguyên, không clamp ở 100%.

Điều kiện pass cố định:

    95% <= lượng nước thực tế <= 105%

| Kết quả | Điều kiện | Hệ quả |
| --- | --- | --- |
| Pass | 95–105% | Chai qua QC, đi tới splitter/đóng gói. |
| Underfill | <95% | Chai bị QC đánh dấu reject. |
| Overflow | >105% | Chai bị QC đánh dấu reject. |

Overflow không chặn Tightener: chai vẫn được đóng nắp theo chu trình cơ khí, sau đó QC mới phân loại. Khi overflow, lớp nước xanh sáng phủ bên ngoài cả **Bottle Body** và **Bottle Neck**; hai lớp này không có collider và tự tắt khi chai reset.

Ví dụ với 3 nozzle, dwell 1.35 s và bồn còn đủ nước:

    133.33 L/min -> 1.00 L/chai  -> nominal pass
    300.00 L/min -> 2.25 L/chai  -> overflow reject

Nếu KPI **Vessel** là 0 / 150 L, các chai mới không thể nhận thêm nước. Bấm **Reset** để nạp lại bồn; reset giữ setpoint hiện tại, nên có thể giữ Pump flow cao để quan sát overflow ngay từ đầu.

## Reject Sweep Bar thực tế

Reject không teleport hay đứng chờ thanh gạt:

- Khi một chai lỗi đi tới reject station, Sweep Bar bắt đầu một chu trình đi ra và thu về.
- Chai vẫn chạy cùng conveyor trong thời gian đó.
- Trong từng frame của cả hành trình ra và về, hệ thống kiểm tra bounds thực tế của thanh gạt cộng bán kính chai.
- Mọi chai va vào vùng quét đều bị lấy khỏi line và đưa vào reject tray — gồm cả chai lỗi lẫn chai đạt.
- Nếu reject tray đầy giữa một lượt quét, tray xả trước rồi tiếp tục nhận tất cả chai đã va chạm.
- Chai lỗi đi qua hết vùng quét mà không bị chạm vẫn đi qua A/B Split Guide, xếp hàng và theo carton ra khỏi line như một chai thường. Nó chỉ được ghi một lần vào TotalRejectEscapes; không được tính pass hay reject đã loại.

Vì vậy Conveyor nhanh làm tăng khả năng reject escape hoặc loại nhầm, nhưng không trực tiếp làm chai rót thiếu nước.

## Dashboard và KPI

HUD Unity và dashboard web dùng chung snapshot TwinSnapshot.

Các KPI quan trọng:

- Throughput, Average fill, Last batch và Reject rate.
- Mức bồn, số chai trong turntable buffer và trên line.
- Pass, Reject, Overflow và Reject escapes.
- Disc RPM, dwell, thời gian index/pocket và phase hiện tại của Star Wheel.
- Tốc độ góc và gia tốc ly tâm của turntable.

Alert hiện hành có thể báo:

- Overflow detected
- Low vessel level
- Reject escape detected
- High reject rate
- Turntable buffer near full
- Underfill detected

## Experiment presets

Mỗi preset bắt đầu từ bộ setpoint mặc định, không cộng dồn với preset trước đó.

| Preset | Thiết lập thử nghiệm |
| --- | --- |
| Nominal | Trở về bộ mặc định. |
| High conveyor | Tăng Conveyor lên 1.65 lần để quan sát reject escape/va chạm thực tế. |
| Low pump flow | Giảm Pump flow còn 55% để tạo underfill. |
| High infeed RPM | Tăng infeed RPM lên 1.65 lần. |
| Fast Disc index | Disc index speed = 30 rpm. |
| Slow Disc index | Disc index speed = 2 rpm. |
| Short Disc dwell | Disc dwell = 0.35 s. |
| Long Disc dwell | Disc dwell = 3.50 s. |
| Overflow pump test | Pump flow = 300 L/min, giữ dwell mặc định để tạo overflow rõ ràng. |

Preset chỉ đổi setpoint. Nếu muốn làm lại một thử nghiệm với bồn đầy và bộ đếm mới, chọn preset rồi bấm **Reset**. Nút **New seed + reset** thay seed ngẫu nhiên cho lần chạy mô phỏng tiếp theo.

## Hành vi chất lượng cần lưu ý

- Pump flow, lượng bồn và Disc dwell là ba biến quyết định trực tiếp lượng nước/chất lượng rót.
- Conveyor speed không nằm trong công thức lượng nước rót.
- Disc index speed làm thay đổi thời gian di chuyển và năng suất chu trình, nhưng không thay đổi khoảng dwell đã cấu hình.
- Average fill có thể vượt 105% vì đây là lượng nước thực tế trước khi QC loại chai overflow.
- Reject rate chỉ tính chai đã được phân loại pass/reject; reject escape có KPI riêng.

## Dựng scene và tinh chỉnh hình học

Chọn object **Filling Filtering Demo Bootstrap** trong Inspector để tinh chỉnh các tham số dựng scene, sau đó gọi **Rebuild Filling & Filtering Demo** hoặc menu **Tools > Conveyor Twin > Build Demo Scene**.

| Nhóm | Tham số | Tác dụng |
| --- | --- | --- |
| Bottle | Bottle height scale | Scale chiều cao chai trong khoảng kiểm chứng 0.80–1.00, giữ đáy chai trên bề mặt đỡ. |
| Infeed tail curve | Start Z, start tangent, end control offset, segments | Điều chỉnh đoạn cong cuối của cặp rail, từ transfer path tới pocket vào Star Wheel. |
| Star Wheel barrier | Entry lead, opening degrees, segments | Điều chỉnh vị trí/khe mở và độ mịn của barrier bao quanh Disc. |

Lệnh Build Demo Scene dựng lại phần scene sinh tự động và lưu vào SampleScene.unity.

## Kiến trúc mã nguồn

| Tệp | Vai trò |
| --- | --- |
| [Assets/Scripts/ConveyorDemoRuntimeBootstrap.cs](Assets/Scripts/ConveyorDemoRuntimeBootstrap.cs) | Dựng các mesh/station, chai, nozzle, Disc, reject tray và HUD. |
| [Assets/Scripts/FillingFilteringDigitalTwin.cs](Assets/Scripts/FillingFilteringDigitalTwin.cs) | Logic infeed, index/dwell, rót, capping, QC, reject sweep, splitter và đóng gói. |
| [Assets/Scripts/BottleProcessState.cs](Assets/Scripts/BottleProcessState.cs) | Trạng thái từng chai, lượng nước thực, overflow và hiệu ứng nước ngoài thân/cổ chai. |
| [Assets/Scripts/TwinDashboardData.cs](Assets/Scripts/TwinDashboardData.cs) | Setpoint, snapshot, preset và các hàm toán học thuần. |
| [Assets/Scripts/FillingFilteringHud.cs](Assets/Scripts/FillingFilteringHud.cs) | HUD điều khiển và KPI trong Unity. |
| [Assets/Editor/ConveyorDemoSceneBuilder.cs](Assets/Editor/ConveyorDemoSceneBuilder.cs) | Menu dựng lại demo scene. |
| [Assets/Editor/TwinProcessMathTests.cs](Assets/Editor/TwinProcessMathTests.cs) | Test toán học cho dwell, RPM/index, bơm, fill specification và reject sweep. |

## Kiểm thử

Test Editor gồm các quan hệ chính:

- Dwell độc lập với conveyor speed.
- RPM quyết định thời gian index.
- Lượng bơm bị giới hạn đúng bởi lượng còn trong bồn.
- Pump thấp không đạt lượng danh định trong dwell.
- 95% và 105% pass; dưới 95% hoặc vượt 105% fail.
- Pump 300 L/min trong dwell mặc định có thể tạo overflow.
- Vùng quét Reject Sweep Bar và reject escape được tính theo bounds vật lý.

Chạy test trong Unity qua **Window > General > Test Runner > EditMode**.

## Tài liệu bổ sung

- [docs/nguyen-ly-vat-ly.md](docs/nguyen-ly-vat-ly.md): Các nguyên lý vật lý, công thức động học, thủy động học và va chạm theo 7 quy trình kỹ thuật.
- [docs/so-do-thiet-ke-he-thong.md](docs/so-do-thiet-ke-he-thong.md): Sơ đồ thiết kế kiến trúc phần mềm hệ thống Digital Twin theo chuẩn C4 Model (Context, Container, Component, Code/Class).
- [docs/scalloped-star-wheel-logic.md](docs/scalloped-star-wheel-logic.md): ghi chú hình học và handoff infeed/Star Wheel.
- [docs/bao-cao-ngan.md](docs/bao-cao-ngan.md): báo cáo tóm tắt.

README này là mô tả vận hành hiện tại của digital twin; các tài liệu bổ sung cung cấp cái nhìn chi tiết về lý thuyết vật lý và kiến trúc phần mềm.

