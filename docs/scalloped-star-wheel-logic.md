# Infeed Rail và Scalloped Star Wheel: logic hiện tại

Tài liệu này giải thích đường đi từ turntable tới star wheel trong `Assets/Scripts/FillingFilteringDigitalTwin.cs`. Tổng quan dây chuyền và cơ sở vật lý ở [README](../README.md).

## 1. Mô hình trạng thái

Mỗi chai có `InfeedBottleState`; đây là nguồn chân lý để phân biệt vị trí công nghệ của chai:

```text
DroppingToTurntable
  -> OnTurntable
  -> TransitioningToNeckRail
  -> OnNeckRail
  -> OnStarWheel
```

Không suy luận chai “đang ở rail” chỉ bằng tọa độ `z`. Cách cũ có thể làm một chai vừa quay trên mâm vừa bị xử lý như chai rail, gây hiện tượng xuyên/chồng tại cửa ra.

`BottleQualityStatus` là trạng thái chất lượng riêng: rỗng, rót, đầy, passed, capped, rejected hoặc đã vào bin. Hai hệ trạng thái này phục vụ hai câu hỏi khác nhau: *chai đang ở đâu trong infeed?* và *chai đã qua công đoạn nào?*

## 2. Turntable và bắt rail thật

Turntable cập nhật từng chai `OnTurntable` trong mặt phẳng `X-Z`. Vận tốc chai gồm xu hướng dạt theo bán kính và thành phần bám với vận tốc tiếp tuyến mặt mâm. Bán kính tâm chai bị giới hạn bởi:

```text
R_center_max = R_turntable - R_bottle
```

Sau mỗi bước, `ResolveTurntableBottleSeparation` xử lý các cặp chai turntable và các cặp turntable/rail ở vùng giao. Khoảng cách chuẩn dùng trong phần này là `2 × turntableBottleRadius`.

`TryGetInfeedRailLeftContact` dùng `Collider.ClosestPoint` của `Infeed Neck Support Rail Left`. Khi chạm guide, `ConstrainTurntableBottleAgainstInfeedRail` đưa chai về phía hợp lệ và xóa thành phần vận tốc đi vào rail. Chai chỉ được chuyển khi:

- line không dừng;
- buffer đạt `releaseThreshold`;
- đã qua `releaseIntervalSeconds`;
- `TryGetAvailableInfeedRailCaptureX` tìm được chỗ trống dọc rail.

`Turntable Outlet` vẫn tồn tại để thể hiện cửa hình học, nhưng không quyết định vị trí hay chuyển động chai. Không có coroutine đưa chai tới outlet hoặc snap về đầu rail.

## 3. Chuyển mượt và cao độ ngoài mâm

Khi rail nhận chai, `TryCaptureBottleAtInfeedRail` tạo `InfeedRailTransition` gồm vị trí đầu, đích `X/Z` trên tim rail và thời gian đã trôi. `UpdateInfeedRailTransitions` dùng `SmoothStep` trong `neckRailCaptureTransitionSeconds` (runtime `0,14 s`). Trong thời gian này, chai không bị `MoveBottles` điều khiển lần nữa.

Chuyển cao độ không dùng ngay `NeckRailBottleYAtPosition`. Gọi `d` là khoảng cách phẳng tới tâm mâm:

```text
d = ||p_xz - c_xz||
u = clamp01((d - turntableRadius) / infeedRailDropTravelDistanceM)
s = 3u² - 2u³
y = lerp(y_turntable, y_rail, s)
```

Do đó, chai ở trên rail nhưng vẫn nằm trên mặt turntable khi `d <= turntableRadius`. Nó chỉ bắt đầu hạ sau mép mâm và hoàn tất sau `0,22 m` theo cấu hình runtime. Cách này loại bỏ cảm giác chai “rơi thụt” ngay khi vừa chạm rail.

## 4. Hàng chờ rail và blower

`MoveBottleAlongInfeedNeckRail` chỉ chạy với chai `OnNeckRail`. Vận tốc theo hướng rail được cập nhật như sau:

```text
v_target = clamp(v_min + v_wind, v_min, v_max)
a_feed = neckRailGravityAccelerationMps2 + v_wind · airBlowerAccelerationGain
v_next = MoveTowards(v_now, v_target, a_feed · Δt)
```

`ResolveInfeedRailSpacing` giới hạn tiến độ của chai sau bằng chai ngay trước nó. Nếu star wheel không lấy chai, chai đầu dừng ở entry; các chai sau bị dồn lại theo hàng đợi, còn chai mới trên mâm tiếp tục bị guide giữ lại. Đây là logic spacing xác định, không phải solver lực tiếp xúc 3D.

Blower nozzle ở `(-2,517; 2,000; -0,892)`. Bốn jet chạy theo `+X`, nghiêng xuống `15°`. Góc này là hướng trực quan; tác động vào vận tốc chai đến từ hai tham số gió, không từ mô hình khí động học.

## 5. Điều kiện bắt vào star wheel

`GetFrontBottleOnInfeedRail(true)` chỉ trả về chai có:

- trạng thái `OnNeckRail`;
- chưa filling;
- chưa được gán vào pocket;
- là chai tiến xa nhất dọc rail;
- nằm trong `neckRailWheelCaptureDistanceM` của `FillingEntryX`.

Chai không bị hút từ xa vào wheel. Nếu star wheel bận hoặc rail chưa đủ batch, chai được giữ ở cửa vào và spacing tiếp tục bảo vệ hàng phía sau.

## 6. Star wheel 10 pocket

Vị trí logic một pocket được tính trong mặt phẳng `X-Z`:

```text
angle(slot) = entryAngle + slot · 360° / pocketCount
x = center.x + cos(angle) · pocketRadius
z = center.z + sin(angle) · pocketRadius
y = center.y
```

Với 10 pocket, một index cơ sở là `36°`. Chai vào entry pocket `0`; vùng rót runtime là pocket `1..3`, cap drop ở pocket `5`, capping bắt đầu từ pocket `7`, và exit ở pocket cuối.

Trong lúc index, code nội suy vị trí chai bằng `StarWheelSlotPosition` đồng thời xoay mesh `fillingStarWheel`. Khi đủ batch, ba chai được rót; sau đó wheel index ba pocket để vừa đưa batch cũ đến các trạm sau vừa lấy batch mới.

## 7. Giới hạn của mô hình star wheel

Visual wheel và vị trí chai cùng dùng chung index logic, nhưng chai chưa được parent vào transform của pocket vật lý. Vì vậy đây là mô hình vị trí công nghệ xác định, không phải mô phỏng pocket kẹp chai bằng rigidbody.

Điều này có hai hệ quả:

- độ khớp giữa vị trí chai và khuyết mesh phụ thuộc vào việc giữ các hằng số geometry/slot đồng bộ;
- mô hình không dự báo lực kẹp, trượt trong pocket hoặc va chạm động lực học khi tốc độ cao.

Hướng nâng cấp vật lý hơn là tạo `PhysicalPocket 0..9` làm child của rotating assembly, parent chai vào pocket khi guide bàn giao, rồi unparent tại outfeed. Khi đó các trạm filling/capping/outfeed sẽ kích hoạt theo góc/pocket vật lý thay vì chỉ theo slot logic.

## 8. Kiểm tra khi Play Mode

1. Chai chạm rail trái, chuyển `OnTurntable -> TransitioningToNeckRail -> OnNeckRail` mà không nhảy về base rail.
2. Chai còn cao độ mặt mâm tới khi tâm vượt bán kính turntable; sau đó hạ mượt trong đoạn `0,22 m`.
3. Khi rail đầy, chai mới dừng ở guide và không xuyên qua chai rail.
4. Chỉ chai đầu rail, sát entry pocket, được chuyển `OnStarWheel`.
5. Ba pocket rót đủ chai thì star wheel khóa/rót; chai đã cap được xả ra line để QC.
