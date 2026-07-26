# Báo cáo ngắn: Digital Model dây chuyền Filling & Filtering

## Mục tiêu

Mô hình Unity minh họa dây chuyền chai thủy tinh gồm cấp chai bằng mâm quay, dẫn chai vào star wheel, chiết rót, đóng nắp, QC, tách hai lane và đóng carton sáu chai. Đây là Digital Model phục vụ quan sát luồng vật liệu và kiểm thử logic vận hành, chưa kết nối dữ liệu PLC/IIoT thực.

## Luồng mô phỏng

```text
Dropper -> Turntable -> Outlet Forming Guide -> Conveyor Guide Rails -> Star Wheel
        -> QC -> Reject hoặc A/B splitter -> Six-pack carton
```

- Mâm đặt bên trái lane A tại `X=-1.40`, `Z=-3.35`, quay với Y rotation giảm.
- Cửa ra bên phải có nẹp cố định để gom và định hình chai trước khi bàn giao.
- Lane A kéo dài tới `Z=-4.45`; cặp rail cố định có chân từ nền dẫn chai từ cửa ra theo `+Z` tới pocket 0.
- Chai đi theo guide path ở tốc độ conveyor, hạ từ mặt mâm xuống belt ở đoạn nhập và giữ khoảng cách theo tiến độ path.
- Star wheel 10 pocket vẫn nhận chai ở pocket 0, rót ba chai song song, đóng nắp và xả ra conveyor cho QC.

## Mô hình vật lý và giới hạn

Turntable dùng xấp xỉ động học trong mặt phẳng X-Z với lực dạt hướng tâm và lực bám bề mặt. Mô hình không giải va chạm rigidbody đầy đủ, lực khí nén hoặc CFD.

Hai rail và nẹp cửa ra dùng collider/hình học để thể hiện cơ cấu dẫn chai. Hàng chờ trên guide path được giải bằng spacing xác định; vì vậy mô hình phù hợp để quan sát logic cấp chai, không dùng để suy ra lực nén thực tế giữa các chai.

Không còn blower, air jet hoặc neck support rail. Thay đổi infeed không làm thay đổi mesh, vị trí pocket, logic index, filling, capping hay outfeed của Scalloped Star Wheel.

## Cấu hình demo

| Hạng mục | Giá trị |
| --- | ---: |
| Turntable | `18 rpm`, bán kính `0,95 m` |
| Buffer | 12 chai đầu, tối đa 16, ngưỡng xả 7 |
| Conveyor | `0,85 m/s`, slip runtime 0 |
| Guide handoff | `0,14 s` |
| Star wheel | 10 pocket, `36°/pocket` |
| Filling / capping | 3 vòi / 3 đầu |
# Cap nhat dan chai

Rail trai Segment 3 la chuan ap chai. Rail phai Segment 3 khong con chan de va duoc dat theo do rong than chai; duoi rail cong nhe dua chai vao pocket 0. Thanh chan xien tren mam quay dua chai ve cua ra truoc khi collider outlet forming guide ban giao chai sang belt.

## Cap nhat mam va star wheel

Mam quay va mat slat conveyor nay dong cao do; transfer plate nho thay the outlet forming guide de chai truot vao belt. Slat conveyor luon animate trong Play Mode. Disc/continuous barrier cua Star Wheel ha `0.288m`, va notch pocket doi theo ban kinh than chai.

## Chinh nhanh trong Inspector

Chon `Filling Filtering Demo Bootstrap`: nhom **Infeed tail curve tuning** chinh End Curve 6, nhom **Star wheel continuous barrier tuning** chinh do mo barrier. Sau do dung context menu **Rebuild Filling & Filtering Demo**.
