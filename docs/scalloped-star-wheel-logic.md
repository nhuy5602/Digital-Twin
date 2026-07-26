# Infeed guide path và Scalloped Star Wheel

Tài liệu này mô tả bàn giao chai từ `Infeed Turntable` tới `Scalloped Star Wheel Disc` trong `Assets/Scripts/FillingFilteringDigitalTwin.cs`.

## Trạng thái chai

```text
DroppingToTurntable
  -> OnTurntable
  -> TransitioningToInfeedGuide
  -> OnInfeedGuide
  -> OnStarWheel
```

Không suy luận chai đang ở infeed chỉ từ tọa độ world. `InfeedBottleState` là nguồn chân lý cho pha chuyển tiếp và giúp tách chai trên mâm với chai đã ở guide path.

## Mâm, nẹp cửa ra và guide path

Mâm nằm bên trái lane A, quay với Y rotation giảm; khe an toàn và `Turntable Outlet` ở phía phải của mâm. `Infeed Turntable Outlet Forming Guide` là collider cố định tại cửa ra. Khi một chai turntable chạm guide, `ConstrainTurntableBottleAgainstInfeedGuide` giới hạn vị trí/triệt thành phần vận tốc đi vào guide.

`TryCaptureBottleAtInfeedGuide` chỉ bắt chai khi line sẵn sàng, buffer đạt `releaseThreshold`, đã qua `releaseIntervalSeconds` và đầu guide còn khoảng trống. Chai được nội suy trong `infeedGuideCaptureTransitionSeconds`, sau đó chạy theo một polyline gồm:

1. điểm rời mâm ở phía `+X`;
2. điểm nhập belt và hạ xuống cao độ conveyor;
3. lane A theo `+Z`;
4. điểm kết thúc ngay trước pocket 0.

Hai `Infeed Bottle Guide Rail` cố định chạy dọc polyline đó và có chân xuống nền. Chúng là hình học dẫn hướng; tiến độ chai được điều khiển xác định theo guide path thay vì mô phỏng va chạm rigidbody đầy đủ.

## Hàng chờ và bắt vào wheel

`MoveBottleAlongInfeedGuide` tăng tiến độ theo `ConveyorEffectiveSpeedMps`. `ResolveInfeedGuideSpacing` giới hạn chai sau bằng khoảng cách tối thiểu với chai kế tiếp. Khi star wheel chưa nhận chai, chai đầu dừng ở cuối path và các chai sau xếp hàng phía sau.

`GetFrontBottleOnInfeedGuide(true)` chỉ trả về chai chưa filling, chưa gán pocket, đứng đầu hàng và nằm trong `infeedGuideWheelCaptureDistanceM` của cuối path. Chai này được đưa vào pocket 0 bằng cơ chế bắt hiện có.

## Phần Star Wheel được giữ nguyên

Không thay đổi hình học scalloped disc, 10 pocket, entry pocket 0, góc index, batch ba chai, vị trí filling/capping hoặc logic xả. Sau khi chai đã vào pocket 0, mọi coroutine index/fill/cap tiếp tục chạy như trước.

## Kiểm tra Play Mode

1. Mâm quay với Y giảm, chai dạt ra cửa bên phải và đi qua nẹp cố định.
2. Chai chuyển `OnTurntable -> TransitioningToInfeedGuide -> OnInfeedGuide` mà không snap.
3. Cao độ chai hạ trên đoạn nhập, rồi ổn định ở cao độ belt.
4. Hai rail giữ chai thẳng hàng, không để chồng lấn khi cuối path bị chặn.
5. Chai đầu hàng được đưa vào pocket 0 và star wheel hoàn tất chu trình rót/đóng nắp/xả bình thường.
# Pinched rail channel and smooth handoff

Segment 3 uses the left rail as its datum. The right rail is derived from the shared bottle-body diameter plus the rail thickness, yielding an inside clearance equal to the bottle width; its former floor support is intentionally absent. The bottle guide path is sampled through the descending lead-in and the final curve, with the handoff Hermite tangent matched to conveyor speed. A diagonal turntable deflector constrains bottles toward the outlet guide only; pocket capture remains at pocket 0.

## Level transfer plate

`Infeed Turntable Conveyor Transfer Plate` replaces the outlet forming guide. The turntable and conveyor have one bottle-base elevation, so the sampled lead-in is lateral only and the plate collider performs the infeed capture. The Star Wheel keeps its process pocket positions and indexing, while its visual disc/barrier are lowered by `0.288m` and its pocket notch radius is the bottle-body radius.

## Operator tuning points

On `Filling Filtering Demo Bootstrap`, **Infeed tail curve tuning** controls both End Curve 6 rails. **Star wheel continuous barrier tuning** exposes the barrier opening angle and its entry-side lead angle. Rebuild after changing either group.
