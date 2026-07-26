# Digital Model: Filling & Filtering Line

Mô hình Unity cho dây chuyền chai thủy tinh: cấp chai bằng mâm quay, dẫn vào star wheel, chiết rót, đóng nắp, kiểm tra QC, tách lane và đóng six-pack. Đây là Digital Model: mọi trạng thái vận hành được mô phỏng trong Unity, chưa kết nối PLC hoặc telemetry thực.

## Chạy demo

Yêu cầu Unity 6000.5.0f1.

1. Mở `Assets/Scenes/SampleScene.unity`.
2. Chạy Play Mode.
3. Khi cần dựng lại toàn bộ demo, dùng **Tools > Conveyor Twin > Build Demo Scene**. Lệnh này tạo lại phần scene sinh tự động và lưu đè `SampleScene.unity`.

## Luồng chai

```text
Bottle Dropper
  -> Infeed Turntable (bên trái lane A, Y rotation giảm)
  -> Infeed Turntable Outlet Forming Guide
  -> Infeed Bottle Guide Rail Left + Right
  -> 10-pocket Filling Star Wheel
  -> QC -> Reject hoặc A/B splitter -> six-pack carton
```

- Tâm mâm đặt tại `X=-1.40`, `Z=-3.35`; cửa ra giữ ở phía phải (`+X`).
- Băng chuyền lane A được kéo dài đến `Z=-4.45`.
- Nẹp cố định trong vùng cửa ra giới hạn chai trên mâm và cho phép từng chai chuyển vào đường dẫn.
- Hai rail cố định gồm đoạn nhập từ cửa mâm và đoạn chạy theo `+Z`; mỗi đoạn có chân và đế xuống nền.
- Chai hạ dần từ cao độ mặt mâm xuống cao độ belt trên đoạn nhập, sau đó di chuyển theo tốc độ conveyor và được giữ khoảng cách trên toàn guide path.
- Chai đầu hàng, ở cuối guide path, được đưa vào pocket 0 hiện có của star wheel.

## Logic infeed

`InfeedBottleState` biểu diễn riêng trạng thái vị trí của chai:

```text
DroppingToTurntable -> OnTurntable -> TransitioningToInfeedGuide
-> OnInfeedGuide -> OnStarWheel
```

`Infeed Turntable Outlet Forming Guide` cung cấp collider cho quá trình bắt chai. Sau khi buffer đạt ngưỡng và còn đủ chỗ trên guide path, chai được nội suy sang điểm đầu trong `0.14 s`. Từ đó, tiến độ theo chiều dài guide path là nguồn chân lý để:

- hạ chai từ cao độ mâm xuống băng chuyền;
- giới hạn khoảng cách giữa các chai;
- xác định chai đầu hàng đủ gần pocket 0.

Không có Air Blower, Air Jet hoặc Infeed Neck Support Rail trong scene hay runtime configuration.

## Star wheel

Star wheel giữ nguyên 10 pocket, vị trí pocket 0, cơ chế index, batch rót ba chai, cấp nắp và xả chai. Thay đổi infeed chỉ thay điểm/điều kiện đưa chai vào pocket 0; không thay đổi hình học hay logic vận hành của wheel.

## Thành phần chính

- `Assets/Scripts/ConveyorDemoRuntimeBootstrap.cs`: dựng scene, mâm, conveyor, nẹp cửa ra và hai infeed guide rail.
- `Assets/Scripts/FillingFilteringDigitalTwin.cs`: trạng thái chai, guide path, star wheel, filling, capping, QC và packing.
- `Assets/Scripts/BottleProcessState.cs`: trạng thái công nghệ và infeed của chai.
- `Assets/Editor/ConveyorDemoSceneBuilder.cs`: menu dựng lại scene.

Tài liệu chi tiết về bàn giao infeed và star wheel có tại [docs/scalloped-star-wheel-logic.md](docs/scalloped-star-wheel-logic.md); bản tóm tắt báo cáo có tại [docs/bao-cao-ngan.md](docs/bao-cao-ngan.md).
# Infeed rail alignment update

- `Infeed Bottle Guide Rail Left Segment 3` is the lateral datum. The bottle centre follows its inside face, then uses a short sampled curve to reach pocket 0.
- `Infeed Bottle Guide Rail Right Segment 3` has no floor support. Its inside clearance is calculated from the shared bottle-body diameter, so the two rails remain exactly bottle-width apart.
- `Infeed Turntable Diagonal Bottle Deflector` is a fixed diagonal collider on the turntable; it channels bottles toward the existing outlet forming guide without changing the Star Wheel.

## Level transfer update

- The infeed turntable top and conveyor slats are level. `Infeed Turntable Conveyor Transfer Plate` replaces the former outlet forming guide and is the simulation handoff surface.
- The visible slat-chain animation no longer pauses with the splitter safety state.
- The Scalloped Star Wheel Disc and fixed barrier are lowered by `0.288m`; each scallop radius equals the bottle-body radius (`0.07m`).

## Inspector tuning

Select `Filling Filtering Demo Bootstrap` and use **Infeed tail curve tuning** to change the start, shape, and segment count of the Left/Right End Curve 6. Use **Star wheel continuous barrier tuning** to change `starWheelBarrierOpeningDegrees` (the open gap) and `starWheelBarrierEntryLeadDegrees` (the gap position), then invoke **Rebuild Filling & Filtering Demo**.
