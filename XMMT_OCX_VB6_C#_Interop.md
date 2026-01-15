# VB6 OCX → C# Interop: FFT / Spectrum Draw Summary

## Problem

Port a legacy **VB6 ActiveX OCX** (spectrum display) to C#, specifically driving its `Draw` method with FFT data.

Symptoms during porting:

* FFT event data looked like garbage in C#
* `AxHost` method calls rendered nothing
* Direct `IDispatch.Invoke` initially failed with `DISP_E_TYPEMISMATCH`
* Same OCX worked perfectly in VB6 and C++

## Key Discovery

The OCX uses **classic VB6 pointer smuggling**, not array marshaling.

### Original VB6 signature

```vb
Sub Draw(pFFT As Integer, size As Integer, sampfreq As Integer)
```

Critical details:

* **VB6 `Integer` = 16-bit (`Int16`)**
* Parameters are **ByRef by default**
* VB6 passes `arr(0)` as a **pointer to the first element**
* The OCX dereferences `pFFT` as a `short*` and reads `size` samples

### Proven by working C++ code

```cpp
args[2].vt    = VT_I2 | VT_BYREF;
args[2].piVal = pFFT;   // short*
```

This exactly defines the contract:

* `pFFT` must be passed as **`VT_I2 | VT_BYREF`**
* The BYREF pointer must point **directly to the first FFT sample**
* No SAFEARRAY
* No pointer value in `VT_I4`
* No AxHost marshaling

## Why C# / AxHost Failed

* `AxHost` tries to “protect” against raw pointers
* `ref short` does **not** represent a contiguous buffer
* Automatic marshaling breaks the original VB6 contract
* Incorrect VARIANT layout causes silent failures or `DISP_E_TYPEMISMATCH`

## Correct Architecture (Final)

### 1. FFT Event Sink (IDispatch)

* Receive FFT event (`DISPID 11`)
* Extract:

  * `size` → `VT_I2`
  * `sampfreq` → `VT_I2`
  * `pFFT` → `VT_I2 | VT_BYREF`
* **Immediately copy** unmanaged data:

```csharp
Marshal.Copy(pFFT, managedShortArray, 0, size);
```

* Never keep the unmanaged pointer
* Raise a managed callback:

```csharp
NotifyFftRaw(short[] fft, short size, short sampfreq);
```

### 2. UI / Form Layer

* Receive managed `short[]`
* Pin it briefly
* Call the OCX **via raw `IDispatch.Invoke`**
* Force repaint if needed

```csharp
fixed (short* p = fftRaw)
{
    InvokeSpecDraw(
        axXMMSpec1.GetOcx(),
        (IntPtr)p,
        size,
        sampfreq);
}
```

### 3. IDispatch Draw Call (Critical Part)

#### VARIANT layout (must be exact)

```csharp
[StructLayout(LayoutKind.Explicit, Size = 16)]
struct VARIANT
{
    [FieldOffset(0)]  public ushort vt;
    [FieldOffset(2)]  public ushort wReserved1;
    [FieldOffset(4)]  public ushort wReserved2;
    [FieldOffset(6)]  public ushort wReserved3;
    [FieldOffset(8)]  public short iVal;
    [FieldOffset(8)]  public IntPtr byref;
}
```

#### Argument setup (reversed order!)

```csharp
args[0] = VT_I2            // sampfreq
args[1] = VT_I2            // size
args[2] = VT_I2 | VT_BYREF // pFFT (short*)
```

This **exactly matches** the VB6 + C++ behavior.

## Debugging Breakthrough

The decisive clue was:

```
hr = 0x80020005 (DISP_E_TYPEMISMATCH)
argErr = 2
```

Because COM arguments are reversed, `argErr=2` identified:
➡️ **The first VB parameter (`pFFT`) was marshaled incorrectly**

Fixing the VARIANT type and layout immediately resolved the issue.

## Final Outcome

* FFT data now matches VB6 output exactly
* Spectrum renders correctly via C#
* No AxHost hacks
* No unsafe lifetime bugs
* Behavior matches legacy VB6 and C++ implementations

## Lessons Learned

* VB6 OCXs often rely on **implicit ByRef pointer semantics**
* `VT_I2 | VT_BYREF` is rare but valid
* A single missing VARIANT field can break everything
* When in doubt, **mirror working C++ byte-for-byte**
* “Impossible in C#” usually means “interop contract not yet understood”

---

If you want, next time we can:

* Strip this to a production-clean helper class
* Add a small render throttle
* Or help document the OCX interface itself so nobody ever has to rediscover this again 😉

But for now — **mission accomplished** 🏆
