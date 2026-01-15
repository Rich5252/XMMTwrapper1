Supplied by courtesy of ChatGpt.

What the **CodeSwitch** bits map to in “MMTTY / XMMT control-panel” terms (i.e., what the *button labels* usually mean in the RTTY world), with notes on when they actually do something.

### AFC (Automatic Frequency Control)

**MMTTY auto-tunes the decoder** to track a drifting/off-frequency RTTY signal by adjusting the receive mark/shift (within limits you set in MMTTY setup). Handy for chasing callers slightly off your TX frequency. ([Turbolog][1])

### NET

**Keeps transmit tones “netted” to what you’re receiving** (so if RX tracking moves, TX follows). This is primarily relevant in **AFSK**, and is typically noted as **not working / not meaningful in FSK**. ([Radio Society of Great Britain][2])

### ATC (Automatic Threshold Control)

**Auto-adjusts the slicer/decision threshold** used by the demod/decoder, instead of you manually setting it. Helps when levels/noise vary. ([EasyLog][3])

### BPF (Band Pass Filter)

Enables/disables an **audio band-pass prefilter** ahead of the demodulator to reduce out-of-band noise/QRM. ([EasyLog][3])

### LMS (LMS / Notch)

Enables the **LMS adaptive filter** (Least-Mean-Squares style) in front of the demodulator; commonly used to reduce interference/tones. (MMTTY added an LMS filter early on; UI often labels it “LMS” and groups it with notch features.) ([HamSoft][4])

### SQ (Squelch)

A **noise gate**: suppresses decoding / output when signal level is below the squelch threshold (so you’re not “decoding the noise”). Many integrations expose “SQ” plus a squelch-level slider. ([DF3CB][5])

### REV (Reverse)

**Swaps mark/space (tone reversal)**. Used when you’re tuned to the signal but the text is garbage because the tones are inverted (e.g., wrong sideband or inverted audio chain). ([QSL.net][6])

### UOS (Unshift On Space)

For **Baudot “letters/figures” shifting**: when enabled on receive, **a received SPACE forces the decoder back to LETTERS** (helps recover from noise hits that leave you stuck in FIGS). MMTTY also has TX-side UOS options, but the front-panel “UOS” button is the RX behavior. ([Groups.io][7])

### NOT (Notch)

Enables/disables a **notch filter** (typically configured under the LMS/Notch area of MMTTY setup). Used to notch out an interfering tone/carrier near your RTTY tones. ([EasyLog][3])

### TWO (Two notches)

Turns on the **second notch filter** (i.e., “two notches” mode). MMTTY’s revision history explicitly mentions adding a 2nd notch frequency and a “Two notches” ON setting. ([HamSoft][4])

---

[1]: https://www.turbolog.de/Help/soundcard_based_rtty.htm?utm_source=chatgpt.com "Soundcard Based RTTY"
[2]: https://rsgb.org/main/about-us/committees/hf-contest-committee/getting-starting-in-contesting/3-choosing-your-first-contest/3-1-data-contesting-techniques/?utm_source=chatgpt.com "3.1 Data Contesting Techniques"
[3]: https://www.easylog.com/northamerica/EasyLog%205.6%20Release%20Notes.pdf?utm_source=chatgpt.com "EasyLog 5.6 Release Notes"
[4]: https://hamsoft.ca/pages/mmtty/revision-history.php?utm_source=chatgpt.com "MMTTY Revision History"
[5]: https://df3cb.com/ftdxrc/documentation/operating/rtty.php?utm_source=chatgpt.com "Operating RTTY - FTDXRC"
[6]: https://www.qsl.net/on7dy/info/RTTY%20Basics.htm?utm_source=chatgpt.com "RTTY Basics"
[7]: https://groups.io/g/RTTY/topic/tx_unshift_on_space/79240723?utm_source=chatgpt.com "TX unshift on space? - RTTY"
