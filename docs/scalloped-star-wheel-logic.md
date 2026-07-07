# Scalloped Star Wheel Logic

Tai lieu nay giai thich logic hien tai cua `Scalloped Star Wheel Disc` trong:

`Assets/Scripts/FillingFilteringDigitalTwin.cs`

Muc tieu la de doc code, hieu vi sao chai bi ket/le nhip, va biet can chinh o dau.

## 1. Mo hinh hien tai

Code hien tai dang tach thanh 2 lop logic:

1. **Visual wheel**: mesh `Scalloped Star Wheel Disc` quay bang `fillingStarWheel.localRotation`.
2. **Logic slot**: vi tri chai duoc tinh bang `StarWheelSlotPosition(slotIndex)`.

Quan trong: `slotIndex` hien tai la **vi tri cong nghe co dinh quanh may**, khong phai pocket vat ly that su dang quay tren mesh.

Nghia la:

- `slot 0`: cua chai vao tu rail.
- `slot 1, 2, 3`: vi tri bom nuoc.
- `slot 5`: vi tri tha nap.
- `slot 7, 8, 9`: vi tri xoay/ep nap.
- `slot 9`: vi tri thoat ra conveyor.

Do do, mesh wheel co the quay, nhung chai khong parent vao mesh. Chai duoc code dat toa do moi moi frame.

## 2. Cac bien quan trong

```csharp
public int starWheelPocketCount = 8;
public Vector3 starWheelCenter = new Vector3(0.78f, 0.82f, -0.68f);
public float starWheelPocketRadius = 0.78f;
public float starWheelEntryAngleDegrees = 220f;
public int fillingStationStartPocketIndex = 2;
public int starWheelIndexStepPockets = 3;
public float starWheelIndexDurationSeconds = 0.32f;
public float starWheelExitReleaseLeadDegrees = 12f;
```

Y nghia:

- `starWheelPocketCount`: tong so pocket tren wheel.
- `starWheelCenter`: tam duong tron ma chai di theo.
- `starWheelPocketRadius`: ban kinh tu tam wheel den chai.
- `starWheelEntryAngleDegrees`: goc cua slot 0, tuc cua vao tu rail.
- `starWheelIndexStepPockets`: moi nhip quay bao nhieu pocket. Hien tai la 3.
- `starWheelIndexDurationSeconds`: thoi gian de quay 1 nhip.
- `starWheelExitReleaseLeadDegrees`: chai duoc tach ra conveyor som hon cua thoat bao nhieu do.

## 3. Cach tinh vi tri slot tren wheel

Code:

```csharp
private Vector3 StarWheelSlotPosition(float slotIndex)
{
    var angle = StarWheelSlotAngleDegrees(slotIndex) * Mathf.Deg2Rad;
    return new Vector3(
        starWheelCenter.x + Mathf.Cos(angle) * starWheelPocketRadius,
        starWheelCenter.y,
        starWheelCenter.z + Mathf.Sin(angle) * starWheelPocketRadius);
}

private float StarWheelSlotAngleDegrees(float slotIndex)
{
    return starWheelEntryAngleDegrees + slotIndex * StarWheelStepAngleDegrees;
}
```

Cong thuc:

```text
x = center.x + cos(angle) * radius
z = center.z + sin(angle) * radius
y = center.y
```

Vi Unity dung mat phang ngang `X-Z`, nen wheel quay quanh truc Y.

Neu `starWheelPocketCount = 10`, thi:

```csharp
StarWheelStepAngleDegrees = 360 / 10 = 36 do
```

Khi do:

- `StarWheelSlotPosition(0)`: cua vao.
- `StarWheelSlotPosition(1)`: tram sau do 36 do.
- `StarWheelSlotPosition(2)`: tram sau do 72 do.
- `StarWheelSlotPosition(3)`: tram sau do 108 do.

## 4. Chai truot tren rail

Chai tren rail duoc xu ly trong `MoveBottles()`.

Dieu kien de coi chai dang tren infeed rail:

```csharp
var onInfeedNeckRail = !bottle.fillingCompleted &&
    !fillingSlotAssignments.ContainsKey(bottle) &&
    Mathf.Abs(position.z - neckRailZ) < 0.25f;
```

Neu chai con truoc cua vao wheel thi goi:

```csharp
position = MoveBottleAlongInfeedNeckRail(bottle, position);
```

Ham truot:

```csharp
private Vector3 MoveBottleAlongInfeedNeckRail(BottleProcessState bottle, Vector3 position)
{
    if (!neckRailSlideSpeeds.TryGetValue(bottle, out var slideSpeed))
    {
        slideSpeed = neckRailMinSlideSpeedMps;
    }

    var windSpeed = Mathf.Max(0f, airBlowerWindSpeedMps);
    var targetSpeed = Mathf.Clamp(
        neckRailMinSlideSpeedMps + windSpeed,
        neckRailMinSlideSpeedMps,
        neckRailMaxSlideSpeedMps);

    var acceleration = neckRailGravityAccelerationMps2 + windSpeed * airBlowerAccelerationGain;
    slideSpeed = Mathf.MoveTowards(slideSpeed, targetSpeed, acceleration * Time.deltaTime);
    neckRailSlideSpeeds[bottle] = slideSpeed;

    var nextX = position.x + NeckRailDirection * slideSpeed * Time.deltaTime;
    position.x = HasReachedFillingEntry(nextX) ? FillingEntryX : nextX;
    position.x = ResolveInfeedRailSpacing(bottle, position.x);
    position.y = NeckRailBottleYAtPosition(position.x);
    position.z = neckRailZ;
    return position;
}
```

Chinh tai day neu muon thay doi toc do truot:

```csharp
public float neckRailMinSlideSpeedMps = 0.12f;
public float neckRailMaxSlideSpeedMps = 0.95f;
public float neckRailGravityAccelerationMps2 = 0.72f;
public float airBlowerWindSpeedMps = 0.8f;
public float airBlowerAccelerationGain = 0.8f;
```

Chai khong duoc hut tu xa vao wheel nua. Chai chi duoc bat khi rat gan wheel:

```csharp
public float neckRailWheelCaptureDistanceM = 0.055f;

private bool IsReadyForWheelCapture(float x)
{
    var distanceToEntry = (FillingEntryX - x) * NeckRailDirection;
    return distanceToEntry >= -0.01f && distanceToEntry <= neckRailWheelCaptureDistanceM;
}
```

Neu muon chai phai sat hon nua moi vao wheel, giam:

```csharp
neckRailWheelCaptureDistanceM
```

Vi du:

```csharp
public float neckRailWheelCaptureDistanceM = 0.03f;
```

## 5. Lay chai dau hang tren rail

Ham nay tim chai gan wheel nhat:

```csharp
private BottleProcessState GetFrontBottleOnInfeedRail(bool requireCaptureZone)
{
    BottleProcessState frontBottle = null;
    var frontProgress = float.NegativeInfinity;
    foreach (var bottle in lineBottles)
    {
        if (bottle == null || bottle.fillingCompleted || fillingSlotAssignments.ContainsKey(bottle))
        {
            continue;
        }

        var position = bottle.transform.position;
        if (Mathf.Abs(position.z - neckRailZ) > 0.25f)
        {
            continue;
        }

        if (requireCaptureZone && !IsReadyForWheelCapture(position.x))
        {
            continue;
        }

        var progress = InfeedRailProgress(position.x);
        if (progress > frontProgress)
        {
            frontProgress = progress;
            frontBottle = bottle;
        }
    }

    return frontBottle;
}
```

Neu `requireCaptureZone = true`, chai phai sat wheel moi duoc tra ve.

## 6. Vong dieu khien vao wheel

Moi frame, `Update()` goi:

```csharp
MoveBottles();
TryStartStarWheelFeedFromRail();
```

`MoveBottles()` chi lo cho chai truot va xep hang.

`TryStartStarWheelFeedFromRail()` moi la ham kich hoat wheel quay:

```csharp
private void TryStartStarWheelFeedFromRail()
{
    var frontBottle = GetFrontBottleOnInfeedRail(true);
    var hasBottleWaitingInEntryPocket = !IsStarWheelPocketAvailable(0);
    var hasBottleOnStarWheel = fillingSlotAssignments.Count > 0;
    if (frontBottle == null && !hasBottleWaitingInEntryPocket && !hasBottleOnStarWheel)
    {
        return;
    }

    if (CanIndexStarWheel() || CanCaptureBottleForFilling())
    {
        StartCoroutine(CaptureBottleIntoStarWheel(frontBottle));
        return;
    }

    if (!fillingCaptureBusy && !StarWheelIndexing && IsStarWheelPocketAvailable(0))
    {
        CaptureBottleIntoEntryPocket(frontBottle);
    }
}
```

Dieu kien quay:

```csharp
private bool CanCaptureBottleForFilling()
{
    return !fillingStationBusy &&
           !fillingCaptureBusy &&
           !StarWheelIndexing &&
           fillingSlotAssignments.Count < starWheelPocketCount;
}

private bool CanIndexStarWheel()
{
    return !fillingStationBusy &&
           !fillingCaptureBusy &&
           !StarWheelIndexing &&
           fillingSlotAssignments.Count > 0;
}
```

Diem can chu y:

- Neu `fillingStationBusy = true`, wheel khong tu quay tu `TryStartStarWheelFeedFromRail()`.
- Luc dang bom, day la dung.
- Nhung neu `fillingStationBusy` bi giu qua lau, wheel se dung.

## 7. Ham quay wheel va bat chai trong luc quay

Ham chinh:

```csharp
private IEnumerator IndexStarWheelOnePitchWithRailFeed(
    Dictionary<BottleProcessState, int> indexedBottles,
    int slotDelta)
{
    if (fillingStarWheel == null)
    {
        yield break;
    }

    slotDelta = Mathf.Clamp(slotDelta, 1, Mathf.Max(1, starWheelPocketCount));
    StarWheelIndexing = true;
    starWheelIndexingSince = Time.time;

    var capturedSteps = new HashSet<int>();
    var releasedBottles = new HashSet<BottleProcessState>();
    var startRotation = fillingStarWheel.localRotation;
    var targetRotation = startRotation * Quaternion.Euler(
        0f,
        -slotDelta * StarWheelStepAngleDegrees,
        0f);

    starWheelIndex = (starWheelIndex + slotDelta) % Mathf.Max(1, starWheelPocketCount);
    var elapsed = 0f;
    var duration = Mathf.Max(0.05f, starWheelIndexDurationSeconds);

    TryCaptureBottleFromRailIntoPassingPocket(indexedBottles, capturedSteps, 0, slotDelta);
    while (elapsed < duration)
    {
        elapsed += Time.deltaTime;
        var ratio = Mathf.Clamp01(elapsed / duration);
        var passedStep = Mathf.Min(slotDelta - 1, Mathf.FloorToInt(ratio * slotDelta));
        for (var step = 0; step <= passedStep; step++)
        {
            TryCaptureBottleFromRailIntoPassingPocket(indexedBottles, capturedSteps, step, slotDelta);
        }

        TryReleaseBottlesCrossingExit(indexedBottles, releasedBottles, ratio, slotDelta);
        fillingStarWheel.localRotation = Quaternion.Slerp(startRotation, targetRotation, ratio);
        foreach (var entry in new List<KeyValuePair<BottleProcessState, int>>(indexedBottles))
        {
            var bottle = entry.Key;
            if (bottle != null)
            {
                bottle.transform.position = StarWheelSlotPosition(
                    Mathf.Lerp(entry.Value, entry.Value + slotDelta, ratio));
            }
        }

        yield return null;
    }

    fillingStarWheel.localRotation = targetRotation;
    TryReleaseBottlesCrossingExit(indexedBottles, releasedBottles, 1f, slotDelta);
    foreach (var entry in indexedBottles)
    {
        var bottle = entry.Key;
        if (bottle != null)
        {
            bottle.transform.position = StarWheelSlotPosition(entry.Value + slotDelta);
        }
    }

    StarWheelIndexing = false;
    starWheelIndexingSince = -1f;
}
```

Y nghia:

- `slotDelta = 3`: moi lan quay 3 pocket.
- `targetRotation`: quay visual wheel.
- `StarWheelSlotPosition(Mathf.Lerp(...))`: di chuyen chai theo duong tron logic.
- `TryCaptureBottleFromRailIntoPassingPocket(...)`: co gang bat chai moi vao wheel trong luc quay.
- `TryReleaseBottlesCrossingExit(...)`: co gang xa chai ra conveyor trong luc quay.

## 8. Bat chai tu rail vao wheel

Code:

```csharp
private void TryCaptureBottleFromRailIntoPassingPocket(
    Dictionary<BottleProcessState, int> indexedBottles,
    HashSet<int> capturedSteps,
    int captureStep,
    int slotDelta)
{
    if (capturedSteps.Contains(captureStep))
    {
        return;
    }

    var finalPocketIndex = slotDelta - captureStep;
    if (!IsProjectedPocketAvailable(indexedBottles, finalPocketIndex, slotDelta))
    {
        return;
    }

    var bottle = GetFrontBottleOnInfeedRail(true);
    if (bottle == null)
    {
        return;
    }

    lineBottles.Remove(bottle);
    neckRailSlideSpeeds.Remove(bottle);
    fillingBottles.Add(bottle);
    bottle.transform.position = StarWheelSlotPosition(0);
    indexedBottles[bottle] = -captureStep;
    capturedSteps.Add(captureStep);
}
```

Giai thich:

- `captureStep = 0`: chai bat dau vao khi wheel moi bat dau quay.
- `captureStep = 1`: chai co the vao khi wheel da quay qua 1 pocket.
- `captureStep = 2`: chai co the vao khi wheel da quay qua 2 pocket.

Voi `slotDelta = 3`:

```text
captureStep 0 -> cuoi nhip chai toi pocket 3
captureStep 1 -> cuoi nhip chai toi pocket 2
captureStep 2 -> cuoi nhip chai toi pocket 1
```

Muc tieu la sau mot nhip quay 3 index, pocket 1, 2, 3 co chai de bom.

Diem yeu hien tai:

- Code van dat chai ngay tai `StarWheelSlotPosition(0)`, sau do noi suy theo slot logic.
- Neu muon cuc ky dung vat ly, nen chuyen chai thanh child cua rotating assembly khi pocket vat ly kep chai.
- Hien tai day van la mo phong bang toa do logic, chua phai va cham/co hoc that.

## 9. Logic bom nuoc va nhip dung

Ham bat dau bom:

```csharp
private void TryStartFillingBatch()
{
    if (fillingStationBusy || fillingCaptureBusy || StarWheelIndexing)
    {
        return;
    }

    var batch = GetReadyFillingBatch();
    if (batch.Count < ActiveFillingNozzleCount)
    {
        return;
    }

    StartCoroutine(FillBottleBatch(batch));
}
```

Ham tim batch:

```csharp
private List<BottleProcessState> GetReadyFillingBatch()
{
    var batch = new List<BottleProcessState>();
    for (var pocketIndex = fillingStationStartPocketIndex;
         pocketIndex <= FillingStationEndPocketIndex;
         pocketIndex++)
    {
        BottleProcessState bottleInPocket = null;
        foreach (var entry in fillingSlotAssignments)
        {
            if (entry.Value == pocketIndex)
            {
                bottleInPocket = entry.Key;
                break;
            }
        }

        if (bottleInPocket == null ||
            bottleInPocket.fillingCompleted ||
            Vector3.Distance(bottleInPocket.transform.position, StarWheelSlotPosition(pocketIndex)) > fillingSlotToleranceM)
        {
            batch.Clear();
            return batch;
        }

        batch.Add(bottleInPocket);
    }

    return batch;
}
```

Nhip mong muon:

```text
1. Wheel quay 3 index.
2. Chai vao pocket 1, 2, 3.
3. Wheel dung.
4. FillBottleBatch() bom 3 chai.
5. Bom xong -> AdvanceStarWheelAfterFilling() quay tiep 3 index.
6. Lap lai.
```

Sau khi bom xong:

```csharp
yield return MoveFillingNozzles(activeSprings, springDownPositions, springBasePositions, fillingNozzleMoveSeconds, batch);
yield return AdvanceStarWheelAfterFilling();
fillingStationBusy = false;
fillingStationBusySince = -1f;
TryStartFillingBatch();
```

Quan trong: `fillingStationBusy` dang duoc giu den sau khi `AdvanceStarWheelAfterFilling()` chay xong, de tranh `Update()` chen mot coroutine quay khac vao giua nhip.

## 10. Xa chai tu wheel xuong conveyor

Trong luc wheel quay, code goi:

```csharp
TryReleaseBottlesCrossingExit(indexedBottles, releasedBottles, ratio, slotDelta);
```

Ham xa:

```csharp
private void TryReleaseBottlesCrossingExit(
    Dictionary<BottleProcessState, int> indexedBottles,
    HashSet<BottleProcessState> releasedBottles,
    float ratio,
    int slotDelta)
{
    var releaseLeadSlots = Mathf.Clamp(
        starWheelExitReleaseLeadDegrees / Mathf.Max(1f, StarWheelStepAngleDegrees),
        0f,
        0.85f);

    var releaseThresholdSlot = FillingExitPocketIndex - releaseLeadSlots;
    var bottlesToRelease = new List<BottleProcessState>();
    foreach (var entry in indexedBottles)
    {
        var bottle = entry.Key;
        if (bottle == null || releasedBottles.Contains(bottle))
        {
            continue;
        }

        var targetSlot = entry.Value + slotDelta;
        if (targetSlot < FillingExitPocketIndex)
        {
            continue;
        }

        var currentSlot = Mathf.Lerp(entry.Value, targetSlot, ratio);
        ApplyStarWheelOperationAtSlot(bottle, currentSlot);
        if (currentSlot >= releaseThresholdSlot && bottle.cappingCompleted)
        {
            bottlesToRelease.Add(bottle);
        }
    }

    var finalZ = NextStarWheelReleaseConveyorZ();
    foreach (var bottle in bottlesToRelease)
    {
        releasedBottles.Add(bottle);
        indexedBottles.Remove(bottle);
        StartCoroutine(ReleaseOneFilledBottleToConveyor(bottle, finalZ));
        finalZ += minimumBottleSpacingM;
    }
}
```

Dieu kien de chai duoc xa:

```csharp
currentSlot >= releaseThresholdSlot && bottle.cappingCompleted
```

Neu chai khong thoat, hay kiem tra:

- `bottle.cappingCompleted` da true chua.
- `capDropPocketIndex` va `cappingPocketStartIndex` co dung vi tri khong.
- `FillingExitPocketIndex` co dung pocket thoat khong.
- `starWheelExitReleaseLeadDegrees` co qua nho khong.

Ham dua chai ra conveyor:

```csharp
private IEnumerator ReleaseOneFilledBottleToConveyor(BottleProcessState bottle, float finalZ)
{
    if (bottle == null)
    {
        yield break;
    }

    var tangentStart = bottle.transform.position;
    var tangentEnd = new Vector3(lineX, starWheelCenter.y, finalZ);
    fillingBottles.Remove(bottle);
    fillingSlotAssignments.Remove(bottle);

    var elapsed = 0f;
    var tangentDuration = Mathf.Max(0.08f, starWheelIndexDurationSeconds * 0.35f);
    while (elapsed < tangentDuration)
    {
        elapsed += Time.deltaTime;
        var ratio = Mathf.SmoothStep(0f, 1f, elapsed / tangentDuration);
        bottle.transform.position = Vector3.Lerp(tangentStart, tangentEnd, ratio);
        yield return null;
    }

    bottle.transform.position = tangentEnd;
    lineBottles.Add(bottle);
}
```

Diem yeu hien tai:

- Chai ra conveyor bang `Vector3.Lerp`, chua bam theo rail cong.
- Neu muon muot hon, nen tao `OutfeedGuidePath` gom 2-3 diem va noi suy theo Bezier/tangent curve.
- Hien tai chai duoc them lai vao `lineBottles` sau khi den `tangentEnd`.

## 11. Vi sao logic hien tai de sai nhip

Nhung diem de sai:

1. **Slot logic la co dinh, mesh wheel quay rieng.**
   Neu visual pocket khong trung voi `StarWheelSlotPosition(slotIndex)`, nguoi xem thay chai khong nam dung khuyet.

2. **Chai khong parent vao rotating assembly.**
   Chai chi duoc set position bang code, nen khong thuc su bi pocket kep.

3. **Wheel quay 3 index nhung chai rail phai kip sat cua.**
   Neu rail truot cham hon wheel, `captureStep 1/2` co the khong bat du chai. Khi do pocket 1, 2, 3 khong du 3 chai, `TryStartFillingBatch()` se khong bom.

4. **Thoat chai phu thuoc `cappingCompleted`.**
   Neu cap/drop/tighten logic chua dung slot, chai co the khong bao gio thoat.

## 12. Cach dieu chinh nhanh

### Chai bi hut tu xa vao wheel

Giam:

```csharp
neckRailWheelCaptureDistanceM
```

Vi du:

```csharp
public float neckRailWheelCaptureDistanceM = 0.03f;
```

### Chai khong kip vao 3 pocket sau khi quay

Tang thoi gian quay:

```csharp
public float starWheelIndexDurationSeconds = 1.2f;
```

Hoac tang toc do rail/gio:

```csharp
public float airBlowerWindSpeedMps = 1.2f;
public float neckRailMaxSlideSpeedMps = 1.2f;
```

### Wheel quay qua nhanh

Tang:

```csharp
starWheelIndexDurationSeconds
```

### Wheel khong dung de bom

Kiem tra:

```csharp
fillingStationStartPocketIndex
FillingStationEndPocketIndex
ActiveFillingNozzleCount
fillingSlotAssignments
```

Dieu kien bat buoc de bom:

```text
pocket 1,2,3 co chai
chai chua fillingCompleted
chai dang nam gan dung StarWheelSlotPosition(pocketIndex)
```

### Chai khong thoat ra conveyor

Kiem tra:

```csharp
bottle.cappingCompleted
capDropPocketIndex
cappingPocketStartIndex
starWheelExitReleaseLeadDegrees
```

## 13. Huong sua dung vat ly hon

Neu muon logic dung voi hinh ve may that, nen doi sang mo hinh nay:

1. Pocket vat ly co index rieng, vi du `PhysicalPocket 0..9`.
2. Moi pocket co transform con cua `Scalloped Star Wheel Rotating Assembly`.
3. Khi pocket di qua cua rail, chai duoc parent vao pocket.
4. Chai di theo wheel bang transform parent, khong tinh `StarWheelSlotPosition()` moi frame nua.
5. Khi pocket den tram fill/cap/outfeed, xu ly theo goc/pocket hien tai.
6. Khi den outfeed, unparent chai va dua vao conveyor path.

Khi do logic se gan voi vat ly hon:

```text
rail -> pocket vat ly kep chai -> wheel quay -> station theo goc -> outfeed
```

Thay vi logic hien tai:

```text
rail -> slot logic co dinh -> tinh toa do chai tren duong tron -> visual wheel quay rieng
```

Day la ly do hien tai ban co the thay `Scalloped Star Wheel Disc` quay nhung chai/slot khong cam giac khop hoan toan voi khuyet ban nguyet.
