# Báo cáo ngắn: Digital Model dây chuyền Filling & Filtering

Tài liệu này là bản tóm tắt cho báo cáo môn học. Mô tả đầy đủ, thông số runtime và giới hạn kỹ thuật nằm trong [README](../README.md).

## 1. Mục tiêu

Xây dựng mô hình Unity cho dây chuyền chai nước: cấp chai bằng mâm quay, dẫn chai qua neck support rail, chiết rót, đóng nắp, kiểm tra mức đầy, loại chai lỗi, tách hai lane và đóng carton sáu chai. Mô hình dùng để quan sát luồng vật liệu, thử tham số vận hành và giải thích mối liên hệ giữa mô hình vật lý gần đúng với logic điều khiển.

## 2. Cấp độ Digital Twin

Project hiện là **Digital Model**. Toàn bộ trạng thái chai, tốc độ, mức bồn và kết quả QC được tạo trong Unity. Scene chưa kết nối PLC, sensor thật, IIoT hay CSV vào dây chuyền Filling & Filtering, nên không tuyên bố là Digital Shadow.

Các class telemetry/belt tổng quát trong project là hướng mở rộng. Để thành Digital Shadow, cần đưa dữ liệu thực vào mô hình, đồng bộ thời gian, ánh xạ tag thiết bị, hiệu chuẩn tham số và kiểm chứng độ sai lệch.

## 3. Quy trình mô phỏng

```text
Dropper -> Turntable -> Left Neck Rail + Blower -> Star Wheel
        -> QC -> Reject hoặc A/B splitter -> Six-pack carton
```

- Chai rơi xuống mâm với điểm đáp lệch nhẹ theo chiều cấp chai.
- Mâm quay giữ chai trong buffer. Chai dạt ra ngoài, va chạm với guide rail trái và chỉ vào rail khi còn chỗ trống.
- Chai chuyển mượt sang rail, được blower đẩy dọc theo `+X`, rồi hạ dần xuống cao độ rail sau khi ra ngoài bán kính mâm.
- Star wheel 10 pocket bắt chai sát cửa vào. Ba chai được rót đồng thời, sau đó nhận nắp và được siết bằng ba capping head trong star wheel.
- QC phân loại theo thể tích. Chai lỗi bị pusher loại; chai đạt đi theo chuỗi lane `A,A,A,B,B,B` để tạo lưới `3 x 2` trong carton.

## 4. Các mô hình vật lý và hiện tượng

### Mâm quay

```text
ω = 2π · rpm / 60
v = rω
a_c = rω²
```

`a_c` là gia tốc hướng tâm vật lý, hướng về tâm. Trong mô hình Unity, code dùng thành phần dạt ra ngoài trong hệ quy chiếu quay cùng lực bám bề mặt để tái hiện hiệu ứng chai bị cuốn theo mâm. Đây là xấp xỉ động học, không phải phép giải ma sát/tiếp xúc rigidbody.

Chai dùng bán kính logic `0,11 m`; trên mâm và tại giao rail, các tâm chai được tách theo khoảng cách tối thiểu `2R`. Khi rail bị dồn, chai mới dừng ở guide thay vì xuyên qua hàng chai.

### Rơi, rail và gió

Chuyển động rơi là `lerp` theo thời gian với độ lệch điểm đáp `0,18 m`; không mô phỏng rơi tự do, lực cản hay quỹ đạo parabol.

Trên rail, vận tốc được tăng dần tới vận tốc mục tiêu nhờ tham số gió. `neckRailGravityAccelerationMps2` là gia tốc cấp liệu hiệu dụng; nó không phải trực tiếp `g sin(θ)` vì rail runtime gần như ngang. Bốn air jet nghiêng xuống `15°` để biểu diễn hướng khí, không phải mô phỏng CFD.

Với khoảng cách phẳng từ tâm chai tới tâm mâm là `d`, cao độ chai được chuyển mượt:

```text
u = clamp01((d - R_table) / 0,22)
s = 3u² - 2u³
y = (1 - s)y_table + sy_rail
```

Vì vậy chai giữ cao độ mặt mâm đến `d = 0,95 m`, rồi hạ liên tục trong `0,22 m` tiếp theo.

### Chiết rót và QC

Ba vòi rót tạo batch ba chai. Xác suất rót đủ là `0,9`; chai lỗi nhận mức đầy ngẫu nhiên `0,5–0,6`. Thể tích bồn giảm theo tổng thể tích đã cấp:

```text
LiquidLevel_next = LiquidLevel_now - Σ(Δvolume01 · bottleCapacityLiters)
```

QC dùng ngưỡng:

```text
liquidVolume01 >= 0,95  -> PASS
liquidVolume01 <  0,95  -> REJECT
```

## 5. Cấu hình chạy demo

| Hạng mục | Giá trị |
| --- | ---: |
| Turntable | `18 rpm`, bán kính `0,95 m` |
| Buffer | 12 chai đầu, tối đa 16, ngưỡng xả 7 |
| Conveyor | `0,85 m/s`, slip runtime 0 |
| Star wheel | 10 pocket, `36°/pocket` |
| Filling / capping | 3 vòi / 3 đầu |
| Chuyển sang rail | `0,14 s` |
| Hạ ngoài mâm | `0,22 m` |

## 6. Giới hạn và giá trị sử dụng

Mô hình không tính lực khí nén, áp suất chất lỏng, biến dạng carton, tiếp xúc 3D hay CFD. Star wheel đặt chai theo slot logic; chai chưa được parent vào pocket cơ khí thật. Do đó, kết quả dùng để minh họa, kiểm tra logic và thảo luận thiết kế, không dùng làm giá trị hiệu chuẩn sản xuất.

Giá trị học thuật của project là làm rõ cách kết hợp mô hình trạng thái, công thức cơ học cơ bản, trực quan 3D và chỉ số vận hành. Đây cũng là nền tảng để thêm telemetry, VVUQ và so sánh với dây chuyền thực trong bước tiếp theo.

## 7. Tham khảo

1. [ISO 23247-2:2021 — Reference architecture for manufacturing digital twins](https://www.iso.org/cms/%20render/live/en/sites/isoorg/contents/data/standard/07/87/78743.html).
2. [NIST — Digital Twins for Advanced Manufacturing](https://www.nist.gov/programs-projects/digital-twins-advanced-manufacturing).
3. [OpenStax Physics — Angular velocity and centripetal acceleration](https://openstax.org/books/physics/pages/6-key-equations).
