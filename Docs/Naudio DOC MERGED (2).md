# Playback with ASIO

The `AsioOut` class in NAudio allows you to both play back and record
audio using an ASIO driver. ASIO is a driver format supported by many
popular Digital Audio Workstation (DAW) applications on Windows, and
usually offers very low latency for record and playback.

To use ASIO, you do need a soundcard that has an ASIO driver. Most
professional soundcards have ASIO drivers, but you can also try the
[ASIO4ALL](http://asio4all.com/) driver which enables ASIO for
soundcards that don't have their own native ASIO driver.

The `AsioOut` class is able to play, record or do both simultaneously.
This article covers the scenario where we just want to play audio.

## Opening an ASIO device for playback

To discover the names of the installed ASIO drivers on your system you
use `AsioOut.GetDriverNames()`.

We can use one of those driver names to pass to the constructor of
`AsioOut`

``` c#
var asioOut = new AsioOut(asioDriverName);
```

## Selecting Output Channels

Pro Audio soundcards often support multiple inputs and outputs. We may
want to find out how many output channels are available on the device.
We can get this with:

``` c#
var outputChannels = asioOut.DriverOutputChannelCount;
```

By default, `AsioOut` will send the audio to the first output channels
on your soundcard. So if you play stereo audio through a four channel
soundcard, the samples will come out of the first two channels. If you
wanted it to come out of different channels you can adjust the
`OutputChannelOffset` parameter.

Next, I call `Init`. This lets us pass the `IWaveProvider` or
`ISampleProvider` we want to play. Note that the sample rate of the
`WaveFormat` of the input provider must be one supported by the ASIO
driver. Usually this means 44.1kHz or higher.

``` c#
// optionally, change the starting channel for outputting audio:
asioOut.ChannelOffset = 2;  
asioOut.Init(mySampleProvider);
```

## Start Playback

As `AsioOut` is an implementation of `IWavePlayer` we just need to call
`Play` to start playing.

``` c#
asioOut.Play(); // start playing
```

Note that since ASIO typically works at very low latencies, it's
important that the components that make up your signal chain are able to
provide audio fast enough. If the ASIO buffer size is say 10ms, that
means that every 10ms you need to generate the next 10ms of audio. If
you miss this window, the audio will gitch.

## Stop Playback

We stop recording by calling Stop().

``` c#
asioOut.Stop();
```

As with other NAudio `IWavePlayer` implementations, we'll get a
`PlaybackStopped` event firing when the driver stops.

And of course we should remember to `Dispose` our instance of `AsioOut`
when we're done with it.

``` c#
asioOut.Dispose();
```

# Recording with ASIO

The `AsioOut` class in NAudio allows you to both play back and record
audio using an ASIO driver. ASIO is a driver format supported by many
popular Digital Audio Workstation (DAW) applications on Windows, and
usually offers very low latency for record and playback.

To use ASIO, you do need a soundcard that has an ASIO driver. Most
professional soundcards have ASIO drivers, but you can also try the
[ASIO4ALL](http://asio4all.com/) driver which enables ASIO for
soundcards that don't have their own native ASIO driver.

Often you'll use `AsioOut` to play audio, or to play and record
simultaneously. This article covers the scenario where we just want to
record audio.

## Opening an ASIO device for recording

To discover the names of the installed ASIO drivers on your system you
use `AsioOut.GetDriverNames()`.

We can use one of those driver names to pass to the constructor of
`AsioOut`

``` c#
var asioOut = new AsioOut(asioDriverName);
```

## Selecting Recording Channels

We may want to find out how many input channels are available on the
device. We can get this with:

``` c#
var inputChannels = asioOut.DriverInputChannelCount;
```

By default, ASIO will capture all input channels when you record, but if
you have a multi-input soundcard, this may be overkill. If you want to
select a sub-range of channels to record from, we can set the
`InputChannelOffset` to the first channel to record on. And then here I
set up a `recordChannelCount` variable which I will use when I start
recording. So in this example, I'm recording on channels 4 and 5
(n.b. channel numbers are zero based).

Finally, I call `InitRecordAndPlayback`. This is a little bit ugly and
future versions of NAudio may provide a nicer method, but the first
parameter supplies the audio to be played. We're just recording, so this
is null. The second argument is the number of channels to record
(starting from `InputChannelOffset`). And the third argument is the
desired sample rate. When we're playing we don;t need this as the sample
rate of the input `IWaveProvider` will be used, but since we're just
recording, we do need to specify the desired sample rate.

``` c#
asioOut.InputChannelOffset = 4;
var recordChannelCount = 2;
var sampleRate = 44100;
asioOut.InitRecordAndPlayback(null, recordChannelCount, sampleRate);
```

## Start Recording

We need to subscribe to the `AudioAvailable` event in order to process
audio received in the ASIO buffer callback.

And we kick off recording by calling `Play()`. Yes, again it's not very
intuitively named for the scenario in which we're recording only, but it
basically tells the ASIO driver to start capturing audio and call us on
each buffer swap.

``` c#
asioOut.AudioAvailable += OnAsioOutAudioAvailable;
asioOut.Play(); // start recording
```

## Handle received audio

When we receive audio we get access to the raw ASIO buffers in an
`AsioAudioAvailableEventArgs` object.

Because ASIO is all about ultimate low latency, NAudio provides direct
access to an `IntPtr` array called `InputBuffers` which contains the
recorded buffer for each input channel. It also provides a
`SamplesPerBuffer` property to tell you how many

But there's still a lot of work to be done. ASIO supports many different
recording formats including 24 bit audio where there are 3 bytes per
sample. You need to examine the `AsioSampleType` property of the
`AsioAudioAvailableEventArgs` to know what format each sample is in.

So it can be a lot of work to access these samples in a meaningful
format. NAudio provides a convenience method called
`GetAsInterleavedSamples` to read samples from each input channel, turn
them into IEEE floating point samples, and interleave them so they could
be written to a WAV file. It supports the most common `AsioSampleType`
properties, but not all of them.

Note that this example uses an overload of `GetAsInterleavedSamples`
that always returns a new `float[]`. It's better for memory purposes to
create your own `float[]` up front and pass that in instead.

Here's the simplest handler for `AudioAvailable` that just gets the
audio as floating point samples and writes them to a `WaveFileReader`
that we've set up in advance.

``` c#
void OnAsioOutAudioAvailable(object sender, AsioAudioAvailableEventArgs e)
{
    var samples = e.GetAsInterleavedSamples();
    writer.WriteSamples(samples, 0, samples.Length);
}
```

For a real application, you'd probably want to write your own logic in
here to access the samples and pass them on to whatever processing logic
you need.

## Stop Recording

We stop recording by calling Stop().

``` c#
asioOut.Stop();
```

As with other NAudio `IWavePlayer` implementations, we'll get a
`PlaybackStopped` event firing when the driver stops.

And of course we should remember to `Dispose` our instance of `AsioOut`
when we're done with it.

``` c#
asioOut.Dispose();
```

# Concatenating Audio

When you play audio or render audio to a file, you create a single
`ISampleProvider` or `IWaveProvider` that represents the whole piece of
audio to be played. So playback will continue until you reach the end,
and then stop.

But what if you have two pieces of audio you want to play back to back?
The `ConcatenatingSampleProvider` enables you to schedule one or more
pieces of audio to play one after the other.

Here's a simple example where we have three audio files that are going
to play back to back. Note that the three audio files must have exactly
the same sample rate, channel count and bit depth, because it's not
possible to change those during playback.

``` c#
var first = new AudioFileReader("first.mp3");
var second = new AudioFileReader("second.mp3");
var third = new AudioFileReader("third.mp3");

var playlist = new ConcatenatingSampleProvider(new[] { first, second, third });

// to play:
outputDevice.Init(playlist);
outputDevice.Play();

// ... OR ... to save to file
WaveFileWriter.CreateWaveFile16("playlist.wav", playlist);
```

Note that the `ConcatenatingSampleProvider` does not provide
repositioning. If you want that, you can quite simply copy the code for
`ConcatenatingSampleProvider` and adjust it to allow you to rewind, or
jump to the beginning of one of the inputs, depending on your specific
requirements.

# FollowedBy Extension Helpers

There are some helpful extension methods you can make use of to simplify
concatenating. For example, to append one `ISampleProvider` onto the end
of another, use `FollowedBy`. Under the hood this simply creates a
`ConcatenatingSampleProvider`:

``` c#
var first = new AudioFileReader("first.mp3");
var second = new AudioFileReader("second.mp3");
var playlist = first.FollowedBy(second);
```

You can also provide a duration of silence that you want after the first
sound has finished and before the second begins:

``` c#
var first = new AudioFileReader("first.mp3");
var second = new AudioFileReader("second.mp3");
var playlist = first.FollowedBy(TimeSpan.FromSeconds(1), second);
```

This makes use of an `OffsetSampleProvider` in conjunction with a
`ConcatenatingSampleProvider`

# Convert Between Stereo and Mono

NAudio includes a number of utility classes that can help you to convert
between mono and stereo audio. You can use these whether you are playing
audio live, or whether you are simply converting from one file format to
another.

# Mono to Stereo

If you have a mono input file, and want to convert to stereo, the
`MonoToStereoSampleProvider` allows you to do this. It takes a
`SampleProvider` as input, and has two floating point `LeftVolume` and
`RightVolume` properties, which default to `1.0f`. This means that the
mono input will be copied at 100% volume into both left and right
channels.

If you wanted to route it just to the left channel, you could set
`LeftVolume` to `1.0f` and `RightVolume` to `0.0f`. And if you wanted it
more to the right than the left you might set `LeftVolume` to `0.25f`
and `RightVolume` to `1.0f`.

``` c#
using(var inputReader = new AudioFileReader(monoFilePath))
{
    // convert our mono ISampleProvider to stereo
    var stereo = new MonoToStereoSampleProvider(inputReader);
    stereo.LeftVolume = 0.0f; // silence in left channel
    stereo.RightVolume = 1.0f; // full volume in right channel

    // can either use this for playback:
    myOutputDevice.Init(stereo);
    myOutputDevice.Play();
    // ...

    // ... OR ... could write the stereo audio out to a WAV file
    WaveFileWriter.CreateWaveFile16(outputFilePath, stereo);
}
```

There's also a `MonoToStereoProvider16` that works with 16 bit PCM
`IWaveProvider` inputs and outputs 16 bit PCM. It works very similarly
to `MonoToStereoSampleProvider` otherwise.

# Stereo to Mono

If you have a stereo input file and want to collapse to mono, then the
`StereoToMonoSampleProvider` is what you want. It takes a stereo
`ISampleProvider` as input, and also has a `LeftVolume` and
`RightVolume` property, although the defaults are `0.5f` for each. This
means the left sample will be multiplied by `0.5f` and the right by
`0.5f` and the two are then summed together.

If you want to just keep the left channel and throw away the right,
you'd set `LeftVolume` to 1.0f and `RightVolume` to 0.0f. You could even
sort out an out of phase issue by setting `LeftVolume` to `0.5f` and
`RightVolume` to -0.5f.

Usage is almost exactly the same. Note that some output devices won't
let you play a mono file directly, so this would be more common if you
were creating a mono output file, or if the mono audio was going to be
passed on as a mixer input to `MixingSampleProvider`.

``` c#
using(var inputReader = new AudioFileReader(stereoFilePath))
{
    // convert our stereo ISampleProvider to mono
    var mono = new StereoToMonoSampleProvider(inputReader);
    mono.LeftVolume = 0.0f; // discard the left channel
    mono.RightVolume = 1.0f; // keep the right channel

    // can either use this for playback:
    myOutputDevice.Init(mono);
    myOutputDevice.Play();
    // ...

    // ... OR ... could write the mono audio out to a WAV file
    WaveFileWriter.CreateWaveFile16(outputFilePath, mono);
}
```

There is also a `StereoToMonoProvider16` that works with 16 bit PCM
stereo `IWaveProvider` inputs and emits 16 bit PCM.

# Panning Mono to Stereo

Finally, NAudio offers a `PanningSampleProvider` which allows you to use
customisable panning laws to govern how a mono input signal is placed
into a stereo output signal.

It has a `Pan` property which can be configured between `-1.0f` (fully
left) and `1.0f` (fully right), with `0.0f` being central.

The `PanningStrategy` can be overridden. By default is uses the
`SinPanStrategy`. There is also `SquareRootPanStrategy`,
`LinearPanStrategy` and `StereoBalanceStrategy`, each one operating
slightly differently with regards to how loud central panning is, and
how the sound tapers off as it is panned to each side. You can
experiment to discover which one fits your needs the best.

Usage is very similar to the `MonoToStereoSampleProvider`

``` c#
using(var inputReader = new AudioFileReader(monoFilePath))
{
    // convert our mono ISampleProvider to stereo
    var panner = new PanningSampleProvider(inputReader);
    // override the default pan strategy
    panner.PanStrategy = new SquareRootPanStrategy();
    panner.Pan = -0.5f; // pan 50% left

    // can either use this for playback:
    myOutputDevice.Init(panner);
    myOutputDevice.Play();
    // ...

    // ... OR ... could write the stereo audio out to a WAV file
    WaveFileWriter.CreateWaveFile16(outputFilePath, panner);
}
```

# Convert an MP3 File to a WAV File

In this article I will show a few ways you can convert an MP3 file into
a WAV file with NAudio.

To start with we'll need a couple of file paths, one to the input MP3
file, and one to where we want to put the converted WAV file.

``` c#
var infile = @"C:\Users\Mark\Desktop\example.mp3";
var outfile = @"C:\Users\Mark\Desktop\converted.wav";
```

## Mp3FileReader

The `Mp3FileReader` class uses the ACM MP3 codec that is present on
almost all consumer versions of Windows. However, it is important to
note that some versions of Windows Server do not have this codec
installed without installing the "Desktop Experience" component.

The conversion is straightforward. Open the MP3 file with
`Mp3FileReader` and then pass it to `WaveFileWriter.CreateWaveFile` to
write the converted PCM audio to a WAV file. This will usually be
44.1kHz 16 bit stereo, but uses whatever format the MP3 decoder emits.

``` c#
using(var reader = new Mp3FileReader(infile))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

## MediaFoundationReader

`MediaFoundationReader` is a flexible class that allows you to read any
audio file formats that Media Foundation supports. This typically
includes MP3 on most consumer versions of Windows, but also usually
supports WMA, AAC, MP4 and others. So unless you need to support Windows
XP or are on a version of Windows without any Media Foundation condecs
installed, this is a great choice. Usage is very similar to
`Mp3FileReader`:

``` c#
using(var reader = new MediaFoundationReader(infile))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

## DirectX Media Object

`Mp3FileReaderBase` allows us to plug in alternative MP3 frame decoders.
One option that comes in the box with NAudio is the DirectX Media Object
MP3 codec. Again, this can only be used if you have that codec installed
on Windows, but it comes with most consumer versions of Windows.

Here's how to use the `DmoMp3FrameDecompressor` as a custom frame
decompressor

``` c#
using(var reader = new Mp3FileReaderBase(infile, wf => new DmoMp3FrameDecompressor(wf)))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

## NLayer

The final option is to use [NLayer](https://github.com/naudio/NLayer) as
the decoder for `Mp3FileReader`. NLayer is a fully managed MP3 decoder,
meaning it can run on any version of Windows (or indeed any .NET
platform). You'll need the [NLayer.NAudioSupport nuget
package](https://www.nuget.org/packages/NLayer.NAudioSupport/). But then
you can plug in a fully managed MP3 frame decoder:

``` c#
using (var reader = new Mp3FileReaderBase(infile, wf => new Mp3FrameDecompressor(wf)))
{
    WaveFileWriter.CreateWaveFile(outfile, reader);
}
```

# Enumerate ACM Drivers

ACM drivers are the old Windows API for dealing with compressed audio
that predates Media Foundation. In one sense this means that this is no
longer very important, but sometimes you find that some codecs are more
readily available as ACM codecs instead of Media Foundation Transforms.

The class in NAudio that makes use of ACM codecs is
`WaveFormatConversionStream`. When you construct one you provide it with
a source and a target `WaveFormat`. This will be either going from
compressed audio to PCM (this is a decoder) or from PCM to compressed
(this is an encoder). Its important to not that you can't just pick two
random `WaveFormat` definitions and expect a conversion to be possible.
You can only perform the supported transforms.

That's why it's really useful to be able to enumerate the ACM codecs
installed on your system. You can do that with
`AcmDriver.EnumerateAcmDrivers`. Then you explore the `FormatTags` for
each driver, and from there ask for each format matching that tag with
`driver.GetFormats`.

It is a little complex, but the information you get from doing this is
invaluable in helping you to work out exactly what `WaveFormat` you need
to use to successfully use a codec.

This code sample enumerates through all ACM drivers and prints out
details of their formats.

``` c#
foreach (var driver in AcmDriver.EnumerateAcmDrivers())
{
    StringBuilder builder = new StringBuilder();
    builder.AppendFormat("Long Name: {0}\r\n", driver.LongName);
    builder.AppendFormat("Short Name: {0}\r\n", driver.ShortName);
    builder.AppendFormat("Driver ID: {0}\r\n", driver.DriverId);
    driver.Open();
    builder.AppendFormat("FormatTags:\r\n");
    foreach (AcmFormatTag formatTag in driver.FormatTags)
    {
        builder.AppendFormat("===========================================\r\n");
        builder.AppendFormat("Format Tag {0}: {1}\r\n", formatTag.FormatTagIndex, formatTag.FormatDescription);
        builder.AppendFormat("   Standard Format Count: {0}\r\n", formatTag.StandardFormatsCount);
        builder.AppendFormat("   Support Flags: {0}\r\n", formatTag.SupportFlags);
        builder.AppendFormat("   Format Tag: {0}, Format Size: {1}\r\n", formatTag.FormatTag, formatTag.FormatSize);
        builder.AppendFormat("   Formats:\r\n");
        foreach (AcmFormat format in driver.GetFormats(formatTag))
        {
            builder.AppendFormat("   ===========================================\r\n");
            builder.AppendFormat("   Format {0}: {1}\r\n", format.FormatIndex, format.FormatDescription);
            builder.AppendFormat("      FormatTag: {0}, Support Flags: {1}\r\n", format.FormatTag, format.SupportFlags);
            builder.AppendFormat("      WaveFormat: {0} {1}Hz Channels: {2} Bits: {3} Block Align: {4}, AverageBytesPerSecond: {5} ({6:0.0} kbps), Extra Size: {7}\r\n",
                format.WaveFormat.Encoding, format.WaveFormat.SampleRate, format.WaveFormat.Channels,
                format.WaveFormat.BitsPerSample, format.WaveFormat.BlockAlign, format.WaveFormat.AverageBytesPerSecond,
                (format.WaveFormat.AverageBytesPerSecond * 8) / 1000.0,
                format.WaveFormat.ExtraSize);
            if (format.WaveFormat is WaveFormatExtraData && format.WaveFormat.ExtraSize > 0)
            {
                WaveFormatExtraData wfed = (WaveFormatExtraData)format.WaveFormat;
                builder.Append("      Extra Bytes:\r\n      ");
                for (int n = 0; n < format.WaveFormat.ExtraSize; n++)
                {
                    builder.AppendFormat("{0:X2} ", wfed.ExtraData[n]);
                }
                builder.Append("\r\n");
            }
        }
    }
    driver.Close();
    Console.WriteLine(builder.ToString());
}
```

The output will be quite verbose (especially if you've installed some
additional codecs on your system.) Here's a snippet of the output from
the GSM codec:

    Long Name: Microsoft GSM 6.10 Audio CODEC
    Short Name: Microsoft GSM 6.10
    Driver ID: 48141232
    FormatTags:
    ===========================================
    Format Tag 0: PCM
       Standard Format Count: 8
       Support Flags: Codec
       Format Tag: Pcm, Format Size: 16
       Formats:
       ===========================================
       Format 0: 8.000 kHz, 8 Bit, Mono
          FormatTag: Pcm, Support Flags: Codec
          WaveFormat: Pcm 8000Hz Channels: 1 Bits: 8 Block Align: 1, AverageBytesPerSecond: 8000 (64.0 kbps), Extra Size: 0
       ===========================================
       Format 1: 8.000 kHz, 16 Bit, Mono
          FormatTag: Pcm, Support Flags: Codec
          WaveFormat: Pcm 8000Hz Channels: 1 Bits: 16 Block Align: 2, AverageBytesPerSecond: 16000 (128.0 kbps), Extra Size: 0
       ===========================================
       Format 2: 11.025 kHz, 8 Bit, Mono
          FormatTag: Pcm, Support Flags: Codec
          WaveFormat: Pcm 11025Hz Channels: 1 Bits: 8 Block Align: 1, AverageBytesPerSecond: 11025 (88.2 kbps), Extra Size: 0

And here's an example showing a non-PCM format. Here we can see that for
`DviAdpcm`, the `WaveFormat` structure needs two extra bytes with values
0xF9 and 0x01:

       ===========================================
       Format 1: 8.000 kHz, 4 Bit, Stereo
          FormatTag: DviAdpcm, Support Flags: Codec
          WaveFormat: DviAdpcm 8000Hz Channels: 2 Bits: 4 Block Align: 512, AverageBytesPerSecond: 8110 (64.9 kbps), Extra Size: 2
          Extra Bytes:
          F9 01 

# Enumerate Media Foundation Transforms

The `MediaFoundationReader` and `MediaFoundationEncoder` classes in
NAudio make use of any available Media Foundation Transforms installed
on your computer. It can be useful to enumerate any audio related MFTs
on your computer.

There are three types of audio MFT - effects, decoders and encoders. A
decoder allows you to decode audio compressed in different formats to
PCM. An encoder allows you to encode PCM audio into compressed formats.
An effect modifies audio in some way. The most

You can use `MediaFoundationApi.EnumerateTransforms` to explore

``` c#
var effects = MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEffect);

var decoders = MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioDecoder);

var encoder = MediaFoundationApi.EnumerateTransforms(MediaFoundationTransformCategories.AudioEncoder);
```

These return an `IEnumerable<IMFActivate>`. This is a fairly low-level
interface. Here's some code that will describe an `IMFActivate` by
exploring its attributes:

``` c#
private string DescribeMft(IMFActivate mft)
{
    mft.GetCount(out var attributeCount);
    var sb = new StringBuilder();
    for (int n = 0; n < attributeCount; n++)
    {
        AddAttribute(mft, n, sb);
    }
    return sb.ToString();
}

private static void AddAttribute(IMFActivate mft, int index, StringBuilder sb)
{
    var variantPtr = Marshal.AllocHGlobal(MarshalHelpers.SizeOf<PropVariant>());
    try
    {
        mft.GetItemByIndex(index, out var key, variantPtr);
        var value = MarshalHelpers.PtrToStructure<PropVariant>(variantPtr);
        var propertyName = FieldDescriptionHelper.Describe(typeof (MediaFoundationAttributes), key);
        if (key == MediaFoundationAttributes.MFT_INPUT_TYPES_Attributes ||
            key == MediaFoundationAttributes.MFT_OUTPUT_TYPES_Attributes)
        {
            var types = value.GetBlobAsArrayOf<MFT_REGISTER_TYPE_INFO>();
            sb.AppendFormat("{0}: {1} items:", propertyName, types.Length);
            sb.AppendLine();
            foreach (var t in types)
            {
                sb.AppendFormat("    {0}-{1}",
                    FieldDescriptionHelper.Describe(typeof (MediaTypes), t.guidMajorType),
                    FieldDescriptionHelper.Describe(typeof (AudioSubtypes), t.guidSubtype));
                sb.AppendLine();
            }
        }
        else if (key == MediaFoundationAttributes.MF_TRANSFORM_CATEGORY_Attribute)
        {
            sb.AppendFormat("{0}: {1}", propertyName,
                FieldDescriptionHelper.Describe(typeof (MediaFoundationTransformCategories), (Guid) value.Value));
            sb.AppendLine();
        }
        else if (value.DataType == (VarEnum.VT_VECTOR | VarEnum.VT_UI1))
        {
            var b = (byte[]) value.Value;
            sb.AppendFormat("{0}: Blob of {1} bytes", propertyName, b.Length);
            sb.AppendLine();
        }
        else
        {
            sb.AppendFormat("{0}: {1}", propertyName, value.Value);
            sb.AppendLine();
        }
    }
    finally
    {
        PropVariant.Clear(variantPtr);
        Marshal.FreeHGlobal(variantPtr);
    }
}
```

Here's an example output for an MFT effect. In this case, the Resampler
which is a very useful MFT for changing sample rates:

    Audio Effect
    Name: Resampler MFT
    Input Types: 2 items:
        Audio-PCM
        Audio-IEEE floating-point
    Class identifier: f447b69e-1884-4a7e-8055-346f74d6edb3
    Output Types: 2 items:
        Audio-PCM
        Audio-IEEE floating-point
    Transform Flags: 1
    Transform Category: Audio Effect

Here's an example output for a decoder. This shows Windows 10 can decode
the Opus audio codec:

    Audio Decoder
    Name: Microsoft Opus Audio Decoder MFT
    Input Types: 1 items:
        Audio-0000704f-0000-0010-8000-00aa00389b71
    Class identifier: 63e17c10-2d43-4c42-8fe3-8d8b63e46a6a
    Output Types: 1 items:
        Audio-IEEE floating-point
    Transform Flags: 1
    Transform Category: Audio Decoder

And an encoder. This is another one new to Windows 10 - it comes with a
FLAC encoder:

    Audio Encoder
    Name: Microsoft FLAC Audio Encoder MFT
    Input Types: 1 items:
        Audio-PCM
    Class identifier: 128509e9-c44e-45dc-95e9-c255b8f466a6
    Output Types: 1 items:
        Audio-0000f1ac-0000-0010-8000-00aa00389b71
    Transform Flags: 1
    Transform Category: Audio Encoder

# Enumerating Audio Devices

The technique you use to enumerate audio devices depends on what audio
output (or input) driver type you are using. This article shows the
technique for each supported output device.

# WaveOut or WaveOutEvent

To discover the number of output devices you can use
`WaveOut.DeviceCount`. Then you can call `WaveOut.GetCapabilities`
passing in the index of a device to find out its name (and some basic
information about its capabilities).

Note that you can also pass an index of -1 which is the "audio mapper".
Use this if you want to keep playing audio even when a device is removed
(such as USB headphones being unplugged).

Also note that the `ProductName` retured is limited to 32 characters,
resulting in it often being truncated. This is a limitation of the
underlying Windows API and there is unfortunately no easy way to fix it
in NAudio.

``` c#
for (int n = -1; n < WaveOut.DeviceCount; n++)
{
    var caps = WaveOut.GetCapabilities(n);
    Console.WriteLine($"{n}: {caps.ProductName}");
}
```

Once you've selected the device you want, you can open it by creating an
instance of `WaveOut` or `WaveOutEvent` and specifying it as the
`DeviceNumber`:

``` c#
var outputDevice = new WaveOutEvent() { DeviceNumber = deviceNumber };
```

# WaveIn or WaveInEvent

Getting details of audio capture devices for `WaveIn` is very similar to
for `WaveOut`:

``` c#
for (int n = -1; n < WaveIn.DeviceCount; n++)
{
    var caps = WaveIn.GetCapabilities(n);
    Console.WriteLine($"{n}: {caps.ProductName}");
}
```

Once you've selected the device you want, you can open it by creating an
instance of `WaveIn` or `WaveInEvent` and specifying it as the
`DeviceNumber`:

``` c#
var recordingDevice = new WaveInEvent() { DeviceNumber = deviceNumber };
```

# DirectSoundOut

`DirectSoundOut` exposes the `Devices` static method allowing you to
enumerate through all the output devices. This has the benefit over
`WaveOut` of not having truncated device names:

``` c#
foreach (var dev in DirectSoundOut.Devices)
{
    Console.WriteLine($"{dev.Guid} {dev.ModuleName} {dev.Description}");
}
```

Each device has a Guid, and that can be used to open a specific device:

``` c#
var outputDevice = new DirectSoundOut(deviceGuid);
```

There are also a couple of special device GUIDs you can use to open the
default playback device (`DirectSoundOut.DSDEVID_DefaultPlayback`) or
default voice playback device
(`DirectSoundOut.DSDEVID_DefaultVoicePlayback`)

# WASAPI Devices

WASAPI playback (render) and recording (capture) devices can both be
accessed via the `MMDeviceEnumerator` class. This allows you to
enumerate only the type of devices you want (`DataFlow.Render` or
`DataFlow.Capture` or `DataFlow.All`).

You can also choose whether you want to include devices that are active,
or also include disabled, unplugged or otherwise not present devices
with the `DeviceState` bitmask. Here we show them all:

``` c#
var enumerator = new MMDeviceEnumerator();
foreach (var wasapi in enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.All))
{
    Console.WriteLine($"{wasapi.DataFlow} {wasapi.FriendlyName} {wasapi.DeviceFriendlyName} {wasapi.State}");
}
```

To open the device you want, simply pass the device in to the
appropriate WASAPI class depending on if you are playing back or
recording...

``` c#
var outputDevice = new WasapiOut(mmDevice, ...);
var recordingDevice = new WasapiIn(captureDevice, ...);
var loopbackCapture = new WasapiLoopbackCapture(loopbackDevice);
```

You can also use the MMEnumerator to request what the default device is
for a number of different scenarios (playback or record, and voice,
multimedia or 'console'):

``` c#
enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
```

# ASIO

You can discover the registered ASIO drivers on your system with
`AsioOut.GetDriverNames`. There is no guarantee that the associated
soundcard is currently connected to the system.

``` c#
foreach (var asio in AsioOut.GetDriverNames())
{
    Console.WriteLine(asio);
}
```

You can then use the driver name to open the device:

``` c#
new AsioOut(driverName);
```

# Management Objects

Finally you can use Windows Management Objects to get hold of details of
the sound devices installed. This doesn't map specifically to any of the
NAudio output device types, but can be a source of useful information

``` c#
var objSearcher = new ManagementObjectSearcher(
       "SELECT * FROM Win32_SoundDevice");

var objCollection = objSearcher.Get();
foreach (var d in objCollection)
{
    Console.WriteLine("=====DEVICE====");
    foreach (var p in d.Properties)
    {
        Console.WriteLine($"{p.Name}:{p.Value}");
    }
}
```

# Fading Audio in and out with FadeInOutSampleProvider

The `FadeInOutSampleProvider` offers a simple, basic way to fade audio
in and out.

It follows the decorator pattern common to many `ISampleProvider`
implementations. You pass in the `ISampleProvider` that you want to fade
in and out.

In this example, we'll construct a `FadeInOutSampleProvider` taking its
source from an `AudioFileReader`, and passing the `true` flag to specify
that we want to start with silence, ready for a fade in.

We'll also immediately trigger a fade in over 2 seconds (2000
milliseconds) by calling `BeginFadeIn`.

``` c#
var audio = new AudioFileReader("example.mp3");
var fade = new FadeInOutSampleProvider(audio, true);
fade.BeginFadeIn(2000);
```

Now we can pass our `FadeInOutSampleProvider` to an output device and
start playing. We'll hear the audio fading in over the first two
seconds.

``` c#
var waveOutDevice = new WaveOutEvent();
waveOutDevice.Init(fade);
waveOutDevice.Play();
```

At some point in the future, we might want to fade out, and we can
trigger that with `BeginFadeOut`, again specifying a 2 second fadeout.

``` c#
fade.BeginFadeOut(2000);
```

Once the audio has faded out, the `FadeInOutSampleProvider` continues to
read from its source but emits silence until it reaches its end, or
until you call `BeginFadeIn` again.

### Taking it further

The `FadeInOutSampleProvider` is a very basic fade provider, and you may
want additional features like:

- automatically fading out when you reach the end of the source
- automatically stopping at the end of a fade out
- cross-fading into another input.

You can do this by taking the code for `FadeInOutSampleProvider` and
adapting it.

For example, to automatically fade out at the end of the source, you'd
actually need to read ahead by the duration of the fade (or know in
advance where you want the fade to begin)

These features may be added in the future to NAudio, but don't be afraid
to create your own custom `ISampleProvider` implementations that behave
just how you want.

# Encode to MP3, WMA and AAC with MediaFoundationEncoder

The `MediaFoundationEncoder` class allows you to use any Media
Foundation transforms (MFTs) on your computer to encode files in a
variety of common audio formats including MP3, WMA and AAC. However, not
all versions of Windows will come with these installed. Media Foundation
is available on Windows Vista and above, and Server versions of Windows
do not typically have the Media Foundation codecs installed (you can add
them by installing the "desktop experience" component.

To get started, let's create an audio folder on the desktop and also
create a simple 20 second WAV file that we can use as an input file.
I'll use a combination of the `SignalGenerator` and the `Take` extension
method to feed into `WaveFileWriter.CreateWaveFile16` to do that:

``` c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var testFilePath = Path.Combine(outputFolder, "test.wav");
// create a test file
WaveFileWriter.CreateWaveFile16(testFilePath, new SignalGenerator(44100,2) 
{   Type = SignalGeneratorType.Sweep, 
    Frequency = 500, 
    FrequencyEnd = 3000, 
    Gain = 0.2f, 
    SweepLengthSecs = 20
}.Take(TimeSpan.FromSeconds(20)));
```

## Initialize Media Foundation

We also need to ensure we've initialized Media Foundation. If we forget
this we'll get a `ComException` of `0xC00D36B0`
(`MF_E_PLATFORM_NOT_INITIALIZED`)

``` c#
MediaFoundationApi.Startup();
```

## Converting WAV to WMA

`MediaFoundationEncoder` includes some static helper methods to make
encoding very straightforward. Let's create a WMA file first, as the WMA
encoder is available with almost all versions of Windows. We just need
to call the `EncodeToWma` method, passing in the source audio (a
`WaveFileReader` in our case) and the output file path. We can also
specify a desired bitrate and it will automatically try to find the
bitrate closest to what we ask for.

``` c#
var wmaFilePath = Path.Combine(outputFolder, "test.wma");
using (var reader = new WaveFileReader(testFilePath))
{
    MediaFoundationEncoder.EncodeToWma(reader, wmaFilePath);
}
```

## Converting WAV to AAC

Windows 7 came with an AAC encoder. So we can create MP4 files with AAC
encoded audio in them like this:

``` c#
var aacFilePath = Path.Combine(outputFolder, "test.mp4");
using (var reader = new WaveFileReader(testFilePath))
{
    MediaFoundationEncoder.EncodeToAac(reader, aacFilePath);
}
```

### Converting WAV to MP3

Windows 8 came with an MP3 encoder. So we can also convert our WAV file
to MP3. This time, let's catch the exception if there isn't an available
encoder:

``` c#

var mp3FilePath = Path.Combine(outputFolder, "test.mp3");
using (var reader = new WaveFileReader(testFilePath))
{
    try
    {
        MediaFoundationEncoder.EncodeToMp3(reader, mp3FilePath);
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine(ex.Message);
    }
}
```

## Converting from other input formats

We've used `WaveFileReader` in all our examples so far. But we can use
the same technique using `MediaFoundationReader`. This will allow us to
convert files of a whole variety of types MP3, WMA, AAC, etc into
anything we have an encoder for. Let's convert our WMA file into AAC

``` c'
var aacFilePath2 = Path.Combine(outputFolder, "test2.mp4");
using (var reader = new MediaFoundationReader(wmaFilePath))
{
    MediaFoundationEncoder.EncodeToAac(reader, aacFilePath2);
}
```

## Extracting audio from online video files

As one final example, let's see that we can use `MediaFoundationReader`
to read a video file directly from a URL and then convert its audio to
an Mp3 file:

    var videoUrl = "https://sec.ch9.ms/ch9/0334/cf0bd333-9c8a-431e-bc62-8089aea60334/WhatsCoolFallCreators.mp4";
    var mp3Path2 = Path.Combine(outputFolder, "test2.mp3");
    using (var reader = new MediaFoundationReader(videoUrl))
    {
        MediaFoundationEncoder.EncodeToMp3(reader, mp3Path2);
    }

# MidiEvent types in NAudio

`MidiEvent` is the base class for all MIDI events in NAudio. It has the
following properties:

- **Channel** - the MIDI channel number from 1 to 16
- **DeltaTime** - the number of ticks after the previous event in the
  MIDI file
- **AbsoluteTime** - the number of ticks from the start of the MIDI file
  (calculated by adding the deltas for all previous events)
- **CommandCode** - the `MidiCommandCode` indicating what type of MIDI
  event it is (e.g note on, note off)
  - note that a command code of `NoteOn` may actually be a note off
    message if its velocity is zero

## NoteEvent

`NoteEvent` is used to represent Note Off and Key After Touch messages.
It is also the base class for `NoteOnEvent`.

It has the following properties - **NoteNumber** the MIDI note number in
the range 0-127 - **Velocity** the MIDI note velocity in the range
0-127. If the commanbd codew is NoteOn and the velocity is 0, then most
synthesizers will interpret this as a note off event

## NoteOnEvent

`NoteOnEvent` inherits from `NoteEvent` and adds a property to track the
associated note off event. This makes it easier to adjust the duration
of a note, as the duration is found by comparing absolute times of the
note on and off events. It also makes sure the associated note off event
stays updated if the note number or channel properties change.

- **OffEvent** - a link to the associated note off event
- **NoteLength** - the note length in ticks. Adjusting this value will
  change the absolutetime of the associated note off event

## MetaEvent

`MetaEvent` is the base class for all MIDI meta events. The main
property is **MetaEventType** which indicates which type of MIDI meta
event it is. Most common meta event types have their own specialized
class which are discussed next.

## TextEvent

`TextEvent` is used for all meta events whose data is text. Examples
include markers, copyright messages, lyrics, track names as well as
basic text events. The **Text** property allows you to access the text
in these events.

## KeySignatureEvent

`KeySignatureEvent` exposes the raw `SharpsFlats` and `MajorMinor`
properties.

## TempoEvent

The `TempoEvent` exposes both the raw `MicrosecondsPerQuarterNote` value
from the MIDI event and also converts that into a `Tempo` expressed as
beats per minute.

## TimeSignatureEvent

`TimeSignatureEvent` exposes `Numerator` (number of beats in a bar),
`Denominator` (which is confusingly in 'beat units' so 1 means 2, 2
means 4 (crochet), 3 means 8 (quaver), 4 means 16 and 5 means 32), as
well as `TicksInMetronomeClick` and `No32ndNotesInQuarterNote`.

## Other MIDI Event Types

- SysexEvent
- ChannelAfterTouchEvent
- PatchChangeEvent
- TrackSequenceNumberEvent
- RawMetaevent
- SmpteOffsetEvent
- SequeceSpecificEvent
- PitchWheelChangeEvent

# Exploring MIDI Files with MidiFile

The `MidiFile` class in NAudio allows you to open and examine the MIDI
events in a standard MIDI file. It can also be used to create or update
MIDI files, but this article focuses on reading.

## Opening a MIDI file

Opening a `MidiFile` is as simple as creating a new `MidiFile` object
and passing in the path. You can choose to enable `strictMode` which
will throw exceptions if various faults are found with the file such as
note on events missing a paired note off or controller values out of
range.

``` c#
var strictMode = false;
var mf = new MidiFile(fileName, strictMode);
```

We can discover what MIDI file format the file is (Type 0 or type 1), as
well as how many tracks are present and what the
`DeltaTicksPerQuarterNote` value is.

``` c#
Console.WriteLine("Format {0}, Tracks {1}, Delta Ticks Per Quarter Note {2}",
                mf.FileFormat, mf.Tracks, mf.DeltaTicksPerQuarterNote);
```

## Examining the MIDI events

The MIDI events can be accessed with the `Events` property, passing in
the index of the track whose events you want to access. This gives you a
`MidiEventCollection` you can iterate through.

All the events in the MIDI file will be represented by a class
inheriting from `MidiEvent`. The `MidiFile` class will also have set an
`AbsoluteTime` property on each note, which represents the timestamp of
the MIDI event from the start of file in terms of delta ticks.

For note on events, `MidiFile` will also try to pair up the
corresponding `NoteOffEvent` events. This allows you to see the duration
of each note (which is simply the difference in time between the
absolute time of the `NoteOffEvent` and `NoteOnEvent`.

Each `MidiEvent` has a `ToString` overload with basic information, so we
can print out details of all the events in the file like this. (we don't
print out the `NoteOffEvent` instances, because they are each paired to
a `NoteOnEvent` which reports the duration)

``` c#
for (int n = 0; n < mf.Tracks; n++)
{
    foreach (var midiEvent in mf.Events[n])
    {
        if(!MidiEvent.IsNoteOff(midiEvent))
        {
            Console.WriteLine("{0} {1}\r\n", ToMBT(midiEvent.AbsoluteTime, mf.DeltaTicksPerQuarterNote, timeSignature), midiEvent);
        }
    }
}
```

You'll see that a helper `ToMBT` method is being used above to convert
the `AbsoluteTime` into a more helpful Measures Beats Ticks format.
Here's a basic implementation (that doesn't take into account any
possible time signature events that might take place)

``` c#
private string ToMBT(long eventTime, int ticksPerQuarterNote, TimeSignatureEvent timeSignature)
{
    int beatsPerBar = timeSignature == null ? 4 : timeSignature.Numerator;
    int ticksPerBar = timeSignature == null ? ticksPerQuarterNote * 4 : (timeSignature.Numerator * ticksPerQuarterNote * 4) / (1 << timeSignature.Denominator);
    int ticksPerBeat = ticksPerBar / beatsPerBar;
    long bar = 1 + (eventTime / ticksPerBar);
    long beat = 1 + ((eventTime % ticksPerBar) / ticksPerBeat);
    long tick = eventTime % ticksPerBeat;
    return String.Format("{0}:{1}:{2}", bar, beat, tick);
}
```

Note that to get the `TimeSignatureEvent` needed by this function we can
simply do something like:

``` c#
var timeSignature = mf.Events[0].OfType<TimeSignatureEvent>().FirstOrDefault();
```

# Sending and Receiving MIDI Events

NAudio allows you to send and receive MIDI events from MIDI devices
using the `MidiIn` and `MidiOut` classes.

## Enumerating MIDI Devices

To discover how many devices are present in your system, you can use
`MidiIn.NumberOfDevices` and `MidiOut.NumberOfDevices`. Then you can ask
for information about each device using `MidiIn.DeviceInfo(index)` and
`MidiOut.DeviceInfo(index)`. The `ProductName` property is most useful
as it can be used to populate a combo box allowing users to select the
device they want.

``` c#
for (int device = 0; device < MidiIn.NumberOfDevices; device++)
{
    comboBoxMidiInDevices.Items.Add(MidiIn.DeviceInfo(device).ProductName);
}
if (comboBoxMidiInDevices.Items.Count > 0)
{
    comboBoxMidiInDevices.SelectedIndex = 0;
}
for (int device = 0; device < MidiOut.NumberOfDevices; device++)
{
    comboBoxMidiOutDevices.Items.Add(MidiOut.DeviceInfo(device).ProductName);
}
```

## Receiving MIDI events

To start monitoring incoming MIDI messages we create a new instance of
`MidiIn` passing in the selected device index (zero based). Then we
subscribe to the `MessageReceived` and `ErrorReceived` properties. Then
we call `Start` to actually start receiving messages from the device.

``` c#
midiIn = new MidiIn(selectedDeviceIndex);
midiIn.MessageReceived += midiIn_MessageReceived;
midiIn.ErrorReceived += midiIn_ErrorReceived;
midiIn.Start();
```

Both event handlers provide us with a `MidiInMessageEventArgs` which
provides a `Timestamp` (in milliseconds), the parsed `MidiEvent` as well
as the `RawMessage` (which can be useful if NAudio couldn't interpret
the message)

``` c#
void midiIn_ErrorReceived(object sender, MidiInMessageEventArgs e)
{
    log.WriteError(String.Format("Time {0} Message 0x{1:X8} Event {2}",
        e.Timestamp, e.RawMessage, e.MidiEvent));
}

void midiIn_MessageReceived(object sender, MidiInMessageEventArgs e)
{
    log.WriteInfo(String.Format("Time {0} Message 0x{1:X8} Event {2}",
        e.Timestamp, e.RawMessage, e.MidiEvent));
}
```

To stop monitoring, simply call `Stop` on the MIDI in device. And also
`Dispose` the device if you are finished with it.

``` c#
midiIn.Stop();
midiIn.Dispose();
```

## Sending MIDI events

Sending MIDI events makes use of `MidiOut`. First, create an instance of
`MidiOut` passing in the desired device number:

``` c#
midiOut = new MidiOut(comboBoxMidiOutDevices.SelectedIndex);
```

Then you can create any MIDI messages using classes derived from
`MidiEvent`. For example, you could create a `NoteOnEvent`. Note that
timestamps and durations are ignored in this scenario - they only apply
to events in a MIDI file.

``` c#
int channel = 1;
int noteNumber = 50;
var noteOnEvent = new NoteOnEvent(0, channel, noteNumber, 100, 50);
```

To send the MIDI event, we need to call `GetAsShortMessage` on the
`MidiEvent` and pass the resulting value to `MidiOut.Send`

``` c#
midiOut.Send(noteOnEvent.GetAsShortMessage());
```

When you're done with sending MIDI events, simply `Dispose` the device.

``` c#
midiOut.Dispose();
```

## Sending and Receiving Sysex message events

Sending a Sysex message can be done using MidiOut.SendBuffer(). It is
not necessary to build and send an entire message as a single SendBuffer
call as long as you ensure that the calls are not asynchronously
interleaved.

``` c#
private static void SendSysex(byte[] message)
{
    midiOut.SendBuffer(new byte[] { 0xF0, 0x00, 0x21, 0x1D, 0x01, 0x01 });
    midiOut.SendBuffer(message);
    midiOut.SendBuffer(new byte[] { 0xF7 });
}
```

Receiving Sysex messages requires two actions in addition to the midiIn
handling above: (1) Allocate a number of buffers each large enough to
receive an expected Sysex message from the device. (2) Subscribe to the
SysexMessageReceived EventHandler property:

``` c#
midiIn = new MidiIn(selectedDeviceIndex);
midiIn.MessageReceived += midiIn_MessageReceived;
midiIn.ErrorReceived += midiIn_ErrorReceived;
midiIn.CreateSysexBuffers(BufferSize, NumberOfBuffers);
midiIn.SysexMessageReceived += midiIn_SysexMessageReceived;
midiIn.Start();
```

The second parameter to the SysexMessageReceived EventHandler is of type
MidiInSysexMessageEventArgs, which has a SysexBytes byte array property:

``` c#
static void midiIn_SysexMessageReceived(object sender, MidiInSysexMessageEventArgs e)
{
    byte[] sysexMessage = e.SysexBytes;
    ....
```

# Mix Two Audio Files into a WAV File

In this tutorial we will mix two audio files together into a WAV file.
The input files can be any supported format such as WAV or MP3.

First, we should open the two input files. We'll use `AudioFileReader`
to do this.

Next, we'll use `MixingSampleProvider` to mix them together. This
expects an `IEnumerable<ISampleProvider>` which

Finally, we use `WaveFileWriter.CreateWaveFile16` passing in the
`MixingSampleProvider` to mix the two files together and output a 16 bit
WAV file.

``` c#
using(var reader1 = new AudioFileReader("file1.wav"))
using(var reader2 = new AudioFileReader("file2.wav"))
{
    var mixer = new MixingSampleProvider(new[] { reader1, reader2 });
    WaveFileWriter.CreateWaveFile16("mixed.wav", mixer);
}
```

Note that there is potential for audio to clip. If the two files are
both loud, then the combined value of a sample will be greater than
1.0f. These have to be clipped before converting back to 16 bit PCM.
This can be fixed by reducing the volume of the inputs. Here's how we
could set the volumes to 75% before mixing

``` c#
reader1.Volume = 0.75f;
reader2.Volume = 0.75f;
```

Alternatively, if we'd used `WaveFileWriter.CreateWaveFile` instead,
then the output would contain IEEE floating point samples instead of 16
bit PCM. This would result in a file twice as large, but any sample
values \> 1.0f would be left intact.

# Using OffsetSampleProvider

`OffsetSampleProvider` allows you to extract a sub-section of another
`ISampleProvider`. You can skip over the start of the source
`ISampleProvider` with `SkipOver` and limit how much audio you play from
the source with `Take`. You can also insert leading and trailing silence
with `DelayBy` and `LeadOut`.

`Take` is especially useful when working with never-ending
`ISampleProvider` sources such as `SignalGenerator`.

Let's look at an example. Here, the `OffsetSampleProvider` uses a
`SignalGenerator` as its source. It inserts 1 second of silence before
playing for 5 seconds and then inserts 1 extra second of silence at the
end:

``` c#
// the source ISampleProvider
var sineWave = new SignalGenerator() { 
    Gain = 0.2, 
    Frequency = 500,
    Type = SignalGeneratorType.Sin};
var trimmed = new OffsetSampleProvider(sineWave) {
    DelayBy = TimeSpan.FromSeconds(1),
    Take = TimeSpan.FromSeconds(5),
    LeadOut = TimeSpan.FromSeconds(1)
};
```

For another example, let's say we have an audio file and we want to skip
over the first one minute, and then take a 30 second excerpt and write
it to a WAV file:

``` c#
var source = new AudioFileReader("example.mp3");
var trimmed = new OffsetSampleProvider(source) {
    SkipOver = TimeSpan.FromSeconds(30),
    Take = TimeSpan.FromSeconds(60),
WaveFileWriter.CreateWaveFile16(outputFilePath, trimmed);
```

## Skip and Take Extension Methods

NAudio also offers some helpful extension methods to simplify the above
task. Skip and Take are extension methods on `ISampleProvider` and
create an `OffsetSampleProvider` behind the scenes. So the previous
example could be rewritten:

``` c#
var trimmed = new AudioFileReader("example.mp3")
                   .Skip(TimeSpan.FromSeconds(30))
                   .Take(TimeSpan.FromSeconds(60));
WaveFileWriter.CreateWaveFile16(outputFilePath, trimmed);
```

## Optimizing SkipOver

Note that `SkipOver` is implemented by simply reading that amount of
audio from the source and discarding it. Obviously if the source is a
file as in this example, it would be more efficient just to position it
to the desired starting point:

``` c#
var source = new AudioFileReader("example.mp3");
source.CurrentTime = TimeSpan.FromSeconds(30);
var trimmed = source.Take(TimeSpan.FromSeconds(60));
WaveFileWriter.CreateWaveFile16(outputFilePath, trimmed);
```

## Sample Accurate Trimming

As well as the TimeSpan based versions of the `SkipOver`, `DelayBy`
`Take` and `LeadOut` properties, there are sample based ones, for when
you need accurate control over exactly how many samples of audio to skip
and take. These are called `SkipOverSamples`, `DelayBySamples`,
`TakeSamples` and `LeadOutSamples`. They're calculated automatically for
you when you use the `TimeSpan` based properties, but you can set them
directly yourself.

# Understanding Output Devices

NAudio supplies wrappers for four different audio output APIs. In
addition, some of them support several different modes of operation.
This can be confusing for those new to NAudio and the various Windows
audio APIs, so in this article I will explain what the four main options
are and when you should use them.

## IWavePlayer

Well start off by discussing the common interface for all output
devices. In NAudio, each output device implements `IWavePlayer`, which
has an `Init` method into which you pass the Wave Provider that will be
supplying the audio data. Then you can call `Play`, `Pause` and `Stop`
which are pretty self-explanatory, except that you need to know that
`Play` only begins playback.

You should only call `Init` once on a given instance of an
`IWavePlayer`. If you need to play something else, you should `Dispose`
of your output device and create a new one.

You will notice there is no capability to get or set the playback
position. That is because the output devices have no concept of position
they just read audio from the `IWaveProvider` supplied until it reaches
an end, at which point the `PlaybackStopped` event is fired.
Alternatively, you can ignore `PlaybackStopped` and just call `Stop`
whenever you decide that playback is no longer required.

You may notice a `Volume` property on the interface that is marked as
`[Obsolete]`. This was marked obsolete because it is not supported on
all device types, but most of them do.

Finally there is a `PlaybackState` property that can report `Stopped`,
`Playing` or `Paused`. Be careful with Stopped though, since if you call
the `Stop` method, the `PlaybackState` will immediately go to `Stopped`
but it may be a few milliseconds before any background playback threads
have actually exited.

## WaveOutEvent & WaveOut

`WaveOutEvent` should be thought of as the default audio output device
in NAudio. If you dont know what to use, choose `WaveOutEvent`. It
essentially wraps the Windows `waveOut` APIs, and is the most
universally supported of all the APIs.

The `WaveOutEvent` (or `WaveOut`) object allows you to configure several
things before you get round to calling `Init`. Most common would be to
change the `DeviceNumber` property. 1 indicates the default output
device, while 0 is the first output device (usually the same in my
experience). To find out how many `WaveOut` output devices are
available, query the static `WaveOut.DeviceCount` property.

You can also set `DesiredLatency`, which is measured in milliseconds.
This figure actually sets the total duration of all the buffers. So in
fact, you could argue that the real latency is shorter. In a future
NAudio, I might reduce confusion by replacing this with a
`BufferDuration` property. By default the `DesiredLatency` is 300ms,
which should ensure a smooth playback experience on most computers. You
can also set the `NumberOfBuffers` to something other than its default
of 2 although 3 is the only other value that is really worth using.

One complication with `WaveOut` is that there are several different
"callback models" available. Understanding which one to use is
important. Callbacks are used whenever `WaveOut` has finished playing
one of its buffers and wants more data. In the callback we read from the
source wave provider and fill a new buffer with the audio. It then
queues it up for playback, assuming there is still more data to play. As
with all output audio driver models, it is imperative that this happens
as quickly as possible, or your output sound will stutter.

### Event Callback

Event callback is the default and recommended approach if you are using
waveOut APIs, and this is implemented in the `WaveOutEvent` class unlike
the other callback options which are accessed via the `WaveOut` class.

The implementation of event callback is similar to WASAPI and
DirectSound. A background thread simply sits around filling up buffers
when they become empty. To help it respond at the right time, an event
handle is set to trigger the background thread that a buffer has been
returned by the soundcard and is in need of filling again.

### New Window Callback

This is a good approach if you are creating a `WaveOut` object from the
GUI thread of a Windows Forms or WPF application. Whenever `WaveOut`
wants more data it posts a message that is handled by the Windows
message pump of an invisible new window. You get this callback model by
default when you call the empty `WaveOut` constructor. However, it will
not work on a background thread, since there is no message pump.

One of the big benefits of using this model (or the Existing Window
model) is that everything happens on the same thread. This protects you
from threading race conditions where a reposition happens at the same
time as a read.

note: The reason for using a new window instead of an existing window is
to eliminate bugs that can happen if you start one playback before a
previous one has finished. It can result in WaveOut picking up messages
it shouldnt.

### Existing Window

Existing Window is essentially the same callback mechanism as New
Window, but you have to pass in the handle of an existing window. This
is passed in as an IntPtr to make it compatible with WPF as well as
WinForms. The only thing to be careful of with this model is using
multiple concurrent instances of WaveOut as they will intercept each
others messages (I may fix this in a future version of NAudio).

note: with both New and Existing Window callback methods, audio playback
will deteriorate if your windows message pump on the GUI thread has too
much other work to do.

### Function Callback

Function callback was the first callback method I attempted to implement
for NAudio, and has proved the most problematic of all callback methods.
Essentially you can give it a function to callback, which seems very
convenient, these callbacks come from a thread within the operating
system.

To complicate matters, some soundcards really dont like two threads
calling waveOut functions at the same time (particularly one calling
waveOutWrite while another calls waveOutReset). This in theory would be
easily fixed with locks around all waveOut calls, but some audio drivers
call the callbacks from another thread while you are calling
waveOutReset, resulting in deadlocks.

Function callbacks should be considered as obsolete now in NAudio, with
`WaveOutEvent` a much better choice.

## DirectSoundOut

DirectSound is a good alternative if for some reason you dont want to
use `WaveOut` since it is simple and widely supported.

To select a specific device with `DirectSoundOut`, you can call the
static `DirectSoundOut.Devices` property which will let you get at the
GUID for each device, which you can pass into the `DirectSoundOut`
constructor. Like `WaveOut`, you can adjust the latency (overall buffer
size).

`DirectSoundOut` uses a background thread waiting to fill buffers (same
as `WaveOutEvent`). This is a reliable and uncomplicated mechanism, but
as with any callback mechanism that uses a background thread, you must
take responsibility yourself for ensuring that repositions do not happen
at the same time as reads (although some of NAudios built-in WaveStreams
can protect you from getting this wrong).

## WasapiOut

WASAPI is the latest and greatest Windows audio API, introduced with
Windows Vista. But just because it is newer doesnt mean you should use
it. In fact, it can be a real pain to use, since it is much more fussy
about the format of the `IWaveProvider` passed to its `Init` function
and will not perform resampling for you.

To select a specific output device, you need to make use of the
`MMDeviceEnumerator` class, which can report the available audio
"endpoints" in the system.

WASAPI out offers you a couple of configuration options. The main one is
whether you open in `shared` or `exclusive` mode. In exclusive mode,
your application requests exclusive access to the soundcard. This is
only recommended if you need to work at very low latencies.

You can also choose whether event callbacks are used. I recommend you do
so, since it enables the background thread to get on with filling a new
buffer as soon as one is needed.

Why would you use WASAPI? I would only recommend it if you want to work
at low latencies or are wanting exclusive use of the soundcard. Remember
that WASAPI is not supported on Windows XP. However, in situations where
WASAPI would be a good choice, ASIO out is often a better one

## AsioOut

ASIO is the de-facto standard for audio interface drivers for recording
studios. All professional audio interfaces for Windows will come with
ASIO drivers that are designed to operate at the lowest latencies
possible. ASIO is probably a better choice than WASAPI for low latency
applications since it is more widely supported (you can use ASIO on XP
for example).

ASIO Out devices are selected by name. Use the AsioOut.GetDriverNames()
to see what devices are available on your system. Note that this will
return all installed ASIO drivers. It does not necessarily mean that the
soundcard is currently connected in the case of an external audio
interface, so `Init` can fail for that reason.

ASIO drivers support their own customised settings GUI. You can access
this by calling `ShowControlPanel()`. Latencies are usually set within
the control panel and are typically specified in samples. Remember that
if you try to work at a really low latency, your input IWaveProviders
`Init` function needs to be really fast.

ASIO drivers can process data in a whole host of native WAV formats
(e.g. big endian vs little endian, 16, 24, 32 bit ints, IEEE floats
etc), not all of which are currently supported by NAudio. If ASIO Out
doesnt work with your soundcard, create an issue on the NAudio GitHub
page, as it is fairly easy to add support for another format.

## Play an Audio File from a Console application

To play a file from a console application, we will use `AudioFileReader`
as a simple way of opening our audio file, and `WaveOutEvent` as the
output device.

We simply need to pass the `audioFile` into the `outputDevice` with the
`Init` method, and then call `Play`.

Since `Play` only means "start playing" and isn't blocking, we can wait
in a loop until playback finishes.

Afterwards, we need to `Dispose` our `audioFile` and `outputDevice`,
which in this example we do by virtue of putting them inside `using`
blocks.

``` c#
using(var audioFile = new AudioFileReader(audioFile))
using(var outputDevice = new WaveOutEvent())
{
    outputDevice.Init(audioFile);
    outputDevice.Play();
    while (outputDevice.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(1000);
    }
}
```

## Play an Audio File from a WinForms application

In this demo, we'll see how to play an audio file from a WinForms
application. This technique will also work

To start with, we'll create a very simple form with a start and a stop
button. And we'll also declare two private members, one to hold the
audio output device (that's the soundcard we're playing out of), and one
to hold the audio file (that's the audio file we're playing).

``` c#
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

public class MainForm : Form
{
    private WaveOutEvent outputDevice;
    private AudioFileReader audioFile;

    public MainForm()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        var flowPanel = new FlowLayoutPanel();
        flowPanel.FlowDirection = FlowDirection.LeftToRight;
        flowPanel.Margin = new Padding(10);

        var buttonPlay = new Button();
        buttonPlay.Text = "Play";
        buttonPlay.Click += OnButtonPlayClick;
        flowPanel.Controls.Add(buttonPlay);

        var buttonStop = new Button();
        buttonStop.Text = "Stop";
        buttonStop.Click += OnButtonStopClick;
        flowPanel.Controls.Add(buttonStop);

        this.Controls.Add(flowPanel);

        this.FormClosing += OnButtonStopClick;
    }
}
```

Now we've not defined the button handlers yet, so let's do that. First
of all the Play button. The first time we click this, we won't have
opened our output device or audio file.

So we'll create an output device of type `WaveOutEvent`. This is only
one of several options for sending audio to the soundcard, but its a
good choice in many scenarios, due to its ease of use and broad platform
support.

We'll also subscribe to the `PlaybackStopped` event, which we can use to
do some cleaning up.

Then if we haven't opened an audio file, we'll use `AudioFileReader` to
load an audio file. This is a good choice as it supports several common
audio file formats including WAV and MP3.

We then tell the output device to play audio from the audio file by
using the `Init` method.

Finally, if all that is done, we can call `Play` on the output device.
This method starts playback but won't wait for it to stop.

``` c#
private void OnButtonPlayClick(object sender, EventArgs args)
{
    if (outputDevice == null)
    {
        outputDevice = new WaveOutEvent();
        outputDevice.PlaybackStopped += OnPlaybackStopped;
    }
    if (audioFile == null)
    {
        audioFile = new AudioFileReader(@"D:\example.mp3");
        outputDevice.Init(audioFile);
    }
    outputDevice.Play();
}
```

We also need a way to request playback to stop. That's in the stop
button click handler, and that's nice and easy. Just call `Stop` on the
output device (if we have one).

``` c#
private void OnButtonStopClick(object sender, EventArgs args)
{
    outputDevice?.Stop();
}
```

Finally, we need to clean up, and the best place to do that is in the
`PlaybackStopped` event handler. Playback can stop for three reasons:

1.  you requested it to stop with `Stop()`
2.  you reached the end of the input file
3.  there was an error (e.g. you removed the USB headphones you were
    listening on)

In the handler for `PlaybackStopped` we'll dispose of both the output
device and the audio file. Of course, you might not want to do this.
Maybe you want the user to carry on playing from where they left off. In
which case you'd not dispose of either. But you would probably want to
reset the `Position` of the `audioFile` to 0, if it had got to the end,
so they could listen again.

``` c#
private void OnPlaybackStopped(object sender, StoppedEventArgs args)
{
    outputDevice.Dispose();
    outputDevice = null;
    audioFile.Dispose();
    audioFile = null;
}
```

And that's it. Congratulations, you've played your first audio file with
NAudio.

## Example 2 - Supporting Rewind and Resume

In this example, we'll use a similar approach, but this time, when we
stop, we won't dispose either the output device or the reader. This
means that next time we press play, we'll resume from where we were when
we stopped.

I've also added a rewind button. This sets the position of the
`AudioFileReader` back to the start by simply setting `Position = 0`

Obviously it is important that when the form is closed we do properly
stop playback and dispose our resources, so we set a `closing` flag to
true when the user shuts down the form. This means that when the
`PlaybackStopped` event fires, we can dispose of the output device and
`AudioFileReader`

Here's the code

``` c#
var wo = new WaveOutEvent();
var af = new AudioFileReader(@"example.mp3");
var closing = false;
wo.PlaybackStopped += (s, a) => { if (closing) { wo.Dispose(); af.Dispose(); } };
wo.Init(af);
var f = new Form();
var b = new Button() { Text = "Play" };
b.Click += (s, a) => wo.Play();
var b2 = new Button() { Text = "Stop", Left=b.Right };
b2.Click += (s, a) => wo.Stop();
var b3 = new Button { Text="Rewind", Left = b2.Right };
b3.Click += (s, a) => af.Position = 0;
f.Controls.Add(b);
f.Controls.Add(b2);
f.Controls.Add(b3);
f.FormClosing += (s, a) => { closing = true; wo.Stop(); };
f.ShowDialog();
```

## Example 3 - Adjusting Volume

In this example, we'll build on the previous one by adding in a volume
slider. We'll use a WinForms `TrackBar` with value between 0 and 100.

When the user moves the trackbar, the `Scroll` event fires and we can
adjust the volume in one of two ways.

First, we can simply change the volume of our output device. It's
important to note that this is a floating point value where 0.0f is
silence and 1.0f is the maximum value. So we'll need to divide the value
of our `TrackBar` by 100.

``` c#
t.Scroll += (s, a) => wo.Volume = t.Value / 100f;
```

Alternatively, the `AudioFileReader` class has a convenient `Volume`
property. This adjusts the value of each sample before it even reaches
the soundcard. This is more work for the code to do, but is very
convenient when you are mixing together multiple files and want to
control their volume individually. The `Volume` property on the
`AudioFileReader` works just the same, going between 0.0 and 1.0. You
can actually provide values greater than 1.0f to this property, to
amplify the audio, but this does result in the potential for clipping.

``` c#
t.Scroll += (s, a) => af.Volume = t.Value / 100f;
```

Let's see the revised version of our form:

``` c#
var wo = new WaveOutEvent();
var af = new AudioFileReader(inputFilePath);
var closing = false;
wo.PlaybackStopped += (s, a) => { if (closing) { wo.Dispose(); af.Dispose(); } };
wo.Init(af);
var f = new Form();
var b = new Button() { Text = "Play" };
b.Click += (s, a) => wo.Play();
var b2 = new Button() { Text = "Stop", Left=b.Right };
b2.Click += (s, a) => wo.Stop();
var b3 = new Button { Text="Rewind", Left = b2.Right };
b3.Click += (s, a) => af.Position = 0;
var t = new TrackBar() { Minimum = 0, Maximum = 100, Value = 100, Top = b.Bottom, TickFrequency = 10 };
t.Scroll += (s, a) => wo.Volume = t.Value / 100f;
// Alternative: t.Scroll += (s, a) => af.Volume = t.Value / 100f;
f.Controls.AddRange(new Control[] { b, b2, b3, t });
f.FormClosing += (s, a) => { closing = true; wo.Stop(); };
f.ShowDialog();
```

# Play Audio From URL

The `MediaFoundationReader` class provides the capability of playing
audio directly from a URL and supports many common audio file formats
such as MP3.

In this example designed to be run from a console app, we use
`MediaFoundationReader` to load the audio from the network and then
simply block until playback has finished.

``` c#
var url = "http://media.ch9.ms/ch9/2876/fd36ef30-cfd2-4558-8412-3cf7a0852876/AzureWebJobs103.mp3";
using(var mf = new MediaFoundationReader(url))
using(var wo = new WasapiOut())
{
    wo.Init(mf);
    wo.Play();
    while (wo.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(1000);
    }
}
```

# Handling Playback Stopped

In NAudio, you use an implementation of the `IWavePlayer` class to play
audio. Examples include `WaveOut`, `WaveOutEvent`, `WasapiOut`,
`AsioOut` etc. To specify the audio to be played, you call the `Init`
method passing in an `IWaveProvider`. And to start playing you call
`Play`.

## Manually Stopping Playback

You can stop audio playback any time by simply calling `Stop`. Depending
on the implementation of `IWavePlayer`, playback may not stop
instantaneously, but finish playing the currently queued buffer (usually
no more than 100ms). So even when you call `Stop`, you should wait for
the `PlaybackStopped` event to be sure that playback has actually
stopped.

## Reaching the end of the input audio

In NAudio, the `Read` method on `IWaveProvider` is called every time the
output device needs more audio to play. The `Read` method should
normally return the requested number of bytes of audio (the `count`
parameter). If `Read` returns less than `count` this means this is the
last piece of audio in the input stream. If `Read` returns 0, the end
has been reached.

NAudio playback devices will stop playing when the `IWaveProvider`'s
`Read` method returns 0. This will cause the `PlaybackStopped` event to
get raised.

## Output device error

If there is any kind of audio error during playback, the
`PlaybackStopped` event will be fired, and the `Exception` property set
to whatever exception caused playback to stop. A very common cause of
this would be playing to a USB device that has been removed during
playback.

## Disposing resources

Often when playback ends, you want to clean up some resources, such as
disposing the output device, and closing any input files such as
`AudioFileReader`. It is strongly recommended that you do this when you
receive the `PlaybackStopped` event and not immediately after calling
`Stop`. This is because in many `IWavePlayer` implementations, the audio
playback code is on another thread, and you may be disposing resources
that will still be used.

Note that NAudio attempts to fire the `PlaybackStopped` event on the
`SynchronizationContext` the device was created on. This means in a
WinForms or WPF application it is safe to access the GUI in the handler.

# Play a Sine Wave

To play a sine wave we can use the `SignalGenerator` class. This can
produce a variety of signal types including sawtooth, pink noise and
triangle waves. We will specify that we want a frequency of 500Hz, and
set the gain to 0.2 (20%). This will help protect us from hurting our
ears.

The `SignalGenerator` will produce a never-ending stream of sound, so
for it to finish, we'd either just call Stop on our output device when
we are happy, or we can se the `Take` extension method, to specify that
we want just the first 20 seconds of sound.

Here's some sample code

``` c#
var sine20Seconds = new SignalGenerator() { 
    Gain = 0.2, 
    Frequency = 500,
    Type = SignalGeneratorType.Sin}
    .Take(TimeSpan.FromSeconds(20));
using (var wo = new WaveOutEvent())
{
    wo.Init(sine20Seconds);
    wo.Play();
    while (wo.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(500);
    }
}
```

# Explore other Signal Types

Signal Generator can produe several other signal types. There are three
other simple repeating signal patterns, for which you can adjust the
gain and signal frequency.

triangle:

    Gain = 0.2, 
    Frequency = 500,
    Type = SignalGeneratorType.Triangle

square:

``` c#
Gain = 0.2, 
Frequency = 500,
Type = SignalGeneratorType.Square
```

and sawtooth:

``` c#
Gain = 0.2, 
Frequency = 500,
Type = SignalGeneratorType.SawTooth
```

There are also two types of noise - pink and white noise. The Frequency
property has no effect:

pink noise

``` c#
Gain = 0.2, 
Type = SignalGeneratorType.PinkNoise
```

white noise:

``` c#
Gain = 0.2, 
Type = SignalGeneratorType.WhiteNoise
```

The final type is the frequency sweep (or 'chirp'). This is a sine wave
that starts at `Frequency` and smoothly ramps up to `FrequencyEnd` over
the period defined in `SweepLengthSecs`. It then returns to the start
frequency and repeats indefinitely

``` c#
Gain = 0.2, 
Frequency = 500, // start frequency of the sweep
FrequencyEnd = 2000, 
Type = SignalGeneratorType.Sweep, 
SweepLengthSecs = 2
```

# Using RawSourceWaveStream

`RawSourceWaveStream` is useful when you have some raw audio, which
might be PCM or compressed, but it is not contained within a file
format. `RawSourceWaveStream` allows you to specify the `WaveFormat`
associated with the raw audio. Let's see some examples.

## Playing from a Byte array

Suppose we have a byte array containing raw 16 bit mono PCM, and want to
play it.

For demo purposes, let's create a 5 second sawtooth wave into the `raw`.
Obviously `SignalGenerator` would be a better way to do this, but we are
simulating getting a byte array from somewhere else, maybe received over
the network.

``` c#
var sampleRate = 16000;
var frequency = 500;
var amplitude = 0.2;
var seconds = 5;

var raw = new byte[sampleRate * seconds * 2];

var multiple = 2.0*frequency/sampleRate;
for (int n = 0; n < sampleRate * seconds; n++)
{
    var sampleSaw = ((n*multiple)%2) - 1;
    var sampleValue = sampleSaw > 0 ? amplitude : -amplitude;
    var sample = (short)(sampleValue * Int16.MaxValue);
    var bytes = BitConverter.GetBytes(sample);
    raw[n*2] = bytes[0];
    raw[n*2 + 1] = bytes[1];
}
```

`RawSourceWaveStream` takes a `Stream` and a `WaveFormat`. The
`WaveFormat` in this instance is 16 bit mono PCM. The stream we can use
`MemoryStream` for, passing in our byte array.

``` c#
var ms = new MemoryStream(raw);
var rs = new RawSourceWaveStream(ms, new WaveFormat(sampleRate, 16, 1));
```

And now we can play the `RawSourceWaveStream` just like it was any other
`WaveStream`:

``` c#
var wo = new WaveOutEvent();
wo.Init(rs);
wo.Play();
while (wo.PlaybackState == PlaybackState.Playing)
{
    Thread.Sleep(500);
}
wo.Dispose();
```

## Turning a raw file into WAV

Suppose we have a raw audio file and we know the wave format of the
audio in it. Let's say its 8kHz 16 bit mono. We can just open the file
with `File.OpenRead` and pass it into a `RawSourceWaveStream`. Then we
can convert it to a WAV file with `WaveFileWriter.CreateWaveFile`.

``` c#
var inPath = "example.pcm";
var outPath = "example.wav";
using(var fileStream = File.OpenRead(inPath))
{
    var s = new RawSourceWaveStream(fileStream, new WaveFormat(8000,1));
    WaveFileWriter.CreateWaveFile(outPath, s);
}
```

Note that WAV files can contain compressed audio, so as long as you know
the correct `WaveFormat` structure you can use that. Let's look at a
compressed audio example next.

## Converting G.729 audio into a PCM WAV

Suppose we have a `.g729` file containing raw audio compressed with
G.729. G.729 isn't actually a built-in `WaveFormat` in NAudio (some
other common ones like mu and a-law are). But we can use
`WaveFormat.CreateCustomFormat` or even derive from `WaveFormat` to
define the correct format.

Now in the previous example we saw how we could create a WAV file that
contains the G.729 audio still encoded. But if we wanted it to be PCM,
we'd need to use `WaveFormatConversionStream.CreatePcmStream` to look
for an ACM codec that understands the incoming `WaveFormat` and can turn
it into PCM.

Please note that this won't always be possible. If your version of
Windows doesn't have a suitable decoder, this will fail.

But here's how we would convert that raw G.729 file into a PCM WAV file
if we did have a suitable decoder:

``` c#
var inFile = @"c:\users\mheath\desktop\chirpg729.g729";
var outFile = @"c:\users\mheath\desktop\chirpg729.wav";
var inFileFormat = WaveFormat.CreateCustomFormat(
            WaveFormatEncoding.G729,
            8000, // sample rate
            1, // channels
            1000, // average bytes per second
            10, // block align
            1); // bits per sample
using(var inStream = File.OpenRead(inFile))
using(var reader = new RawSourceWaveStream(inStream, inFileFormat))
using(var converter = WaveFormatConversionStream.CreatePcmStream(reader))
{
    WaveFileWriter.CreateWaveFile(outFile, converter);
}
```

If it was a format that NAudio has built-in support for like G.711
a-law, then we'd do it like this:

``` c#
var inFile = @"c:\users\mheath\desktop\alaw.bin";
var outFile = @"c:\users\mheath\desktop\alaw.wav";
var inFileFormat = WaveFormat.CreateALawFormat(8000,1);
using(var inStream = File.OpenRead(inFile))
using(var reader = new RawSourceWaveStream(inStream, inFileFormat))
using(var converter = WaveFormatConversionStream.CreatePcmStream(reader))
{
    WaveFileWriter.CreateWaveFile(outFile, converter);
}
```

# Recording Level Meter

In this article we'll see how you can represent the current audio input
level coming from a recording device.

## Start Capturing Audio

In NAudio, the method you call to start capturing audio from an input
device is called `StartRecording`. This method name can cause confusion.
All that it means is that you are asking the input device to provide you
with samples audio. It doesn't mean you are actually recording to an
audio file.

So if you want to allow the user to set up their volume levels before
they start "recording", you'll actually need to call `StartRecording` to
start capturing the audio simply for the purposes of updating the level
meter.

We won't go into great detail in this article on how to record audio as
that's [covered elsewhere](RecordWavFileWinFormsWaveIn.md), but here
we'll create a new recording device, subscribe to the data available
event, and start capturing audio by calling `StartRecording`.

``` c#
var waveIn = new WaveInEvent(deviceNumber);
waveIn.DataAvailable += OnDataAvailable;
waveIn.StartRecording();
```

## Handling Captured Audio

In the `DataAvailable` event handler, if we were simply recording audio,
we'd write to a `WaveFileWriter` like this:

``` c#
private void OnDataAvailable(object sender, WaveInEventArgs args)
{
    writer.Write(args.Buffer, 0, args.BytesRecorded);
};
```

But if we're just letting the user get their levels set up, we'd only
write to the file if the user had actually begun recording. So we might
have a boolean flag that says whether we're recording or not. So when we
get the `DataAvailable` event we don't necessarily write to a file.

``` c#
private void OnDataAvailable(object sender, WaveInEventArgs args)
{
    if (isRecording) 
    {
        writer.Write(args.Buffer, 0, args.BytesRecorded);
    }
};
```

## Calculating Peak Values

The `WaveInEventArgs.Buffer` property contains the captured audio.
Unfortunately this is represented as a byte array. This means that we
must convert to samples.

The way this works depends on the bit depth being recorded at. The two
most common options are 16 bit signed integers (`short`'s in C#), which
is what `WaveIn` and `WaveInEvent` will supply by default. And 32 bit
IEEE floating point numbers (`float`'s in C#) which is what `WasapiIn`
or `WasapiLoopbackCapture` will supply by default.

Here's how we might discover the maximum sample value if the incoming
audio is 16 bit. Notice that we are simply taking the absolute value of
each sample, and we are calculating one maximum value irrespective of
whether it is mono or stereo audio. If you wanted, you could calculate
the maximum values for each channel separately, by maintaining separate
max values for each channel (the samples are interleaved):

``` c#
void OnDataAvailable(object sender, WaveInEventArgs args)
{
    if (isRecording) 
    {
        writer.Write(args.Buffer, 0, args.BytesRecorded);
    }

    float max = 0;
    // interpret as 16 bit audio
    for (int index = 0; index < args.BytesRecorded; index += 2)
    {
        short sample = (short)((args.Buffer[index + 1] << 8) |
                                args.Buffer[index + 0]);
        // to floating point
        var sample32 = sample/32768f;
        // absolute value 
        if (sample32 < 0) sample32 = -sample32;
        // is this the max value?
        if (sample32 > max) max = sample32;
    }
}
```

The previous example showed using bit manipulation, but NAudio also has
a clever trick up its sleeve called `WaveBuffer`. This allows us to
'cast' from a `byte[]` to a `short[]` or `float[]`, something that is
not normally possible in C#.

Here's it working for floating point audio:

``` c#
void OnDataAvailable(object sender, WaveInEventArgs args)
{
    if (isRecording) 
    {
        writer.Write(args.Buffer, 0, args.BytesRecorded);
    }

    float max = 0;
    var buffer = new WaveBuffer(args.Buffer);
    // interpret as 32 bit floating point audio
    for (int index = 0; index < args.BytesRecorded / 4; index++)
    {
        var sample = buffer.FloatBuffer[index];

        // absolute value 
        if (sample < 0) sample = -sample;
        // is this the max value?
        if (sample > max) max = sample;
    }
}
```

The same approach can be used for 16 bit audio, by accessing
`ShortBuffer` instead of `FloatBuffer`.

## Updating the Volume Meter

A very simple way to implement a volume meter in WinForms or WPF is to
use a progressbar. You can set it up with a minimum value of 0 and a
maximum value of 100.

In both our examples, we calulated `max` as a floating point value
between 0.0f and 1.0f, so setting the progressBar value is as simple as:

``` c#
progressBar.Value = 100 * max;
```

Note that you are updating the UI in the `OnDataAvailable` callback.
NAudio will attempt to call this on the UI context if there is one.

Also, this approach means that the frequency of meter updates will match
the size of recording buffers. This is the simplest approach, and
normally works just fine as there will usually be at least 10 buffers
per second which is usually adequate for a volume meter.

# Recording a WAV file in a WinForms app with WaveIn

In this example we'll see how to create a very simple WinForms app that
records audio to a WAV File.

First of all, let's choose where to put the recorded audio. It will go
to a file called `recorded.wav` in a `NAudio` folder on your desktop:

``` c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var outputFilePath = Path.Combine(outputFolder,"recorded.wav");
```

Next, let's create the recording device. I'm going to use `WaveInEvent`
in this case. We could also use `WaveIn` or indeed `WasapiCapture`.

``` c#
var waveIn = new WaveInEvent();
```

I'll declare a `WaveFileWriter` but it won't get created until we start
recording:

``` c#
WaveFileWriter writer = null;
```

And let's set up our form. It will have two buttons - one to start and
one to stop recording. And we'll declare a `closing` flag to allow us to
stop recording when the form is closed.

``` c#
bool closing = false;
var f = new Form();
var buttonRecord = new Button() { Text = "Record" };
var buttonStop = new Button() { Text = "Stop", Left = buttonRecord.Right, Enabled = false };
f.Controls.AddRange(new Control[] { buttonRecord, buttonStop });
```

Now we need some event handlers. When we click `Record`, we'll create a
new `WaveFileWriter`, specifying the path for the WAV file to create and
the format we are recording in. This must be the same as the recording
device format as that is the format we'll receive recorded data in. So
we use `waveIn.WaveFormat`.

Then we start recording with `waveIn.StartRecording()` and set the
button enabled states appropriately.

``` c#
buttonRecord.Click += (s, a) => 
{
    writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat); 
    waveIn.StartRecording(); 
    buttonRecord.Enabled = false; 
    buttonStop.Enabled = true; 
};
```

We also need a handler for the `DataAvailable` event on our input
device. This will start firing periodically after we start recording. We
can just write the buffer in the event args to our writer. Make sure you
write `a.BytesRecorded` bytes, not `a.Buffer.Length`

``` c#
waveIn.DataAvailable += (s, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
};
```

One safety feature I often add when recording WAV is to limit the size
of a WAV file. They grow quickly and can't be over 4GB in any case. Here
I'll request that recording stops after 30 seconds:

``` c#
waveIn.DataAvailable += (s, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
    if (writer.Position > waveIn.WaveFormat.AverageBytesPerSecond * 30)
    {
        waveIn.StopRecording();
    }
};
```

Now we need to handle the stop recording button. This is simple, we just
call `waveIn.StopRecording()`. However, we might still receive more data
in the `DataAvailable` callback, so don't dispose you `WaveFileWriter`
just yet.

``` c#
buttonStop.Click += (s, a) => waveIn.StopRecording();
```

We'll also add a safety measure that if you try to close the form while
you're recording, we'll call `StopRecording` and set a flag so we know
we can also dispose the input device:

``` c#
f.FormClosing += (s, a) => { closing=true; waveIn.StopRecording(); };
```

To safely dispose our `WaveFileWriter`, (which we need to do in order to
produce a valid WAV file), we should handle the `RecordingStopped` event
on our recording device. We `Dispose` the `WaveFileWriter` which fixes
up the headers in our WAV file so that it is valid. Then we set the
button states. Finally, if we're closing the form, the input device
should be disposed.

``` c#
waveIn.RecordingStopped += (s, a) =>
{
    writer?.Dispose(); 
    writer = null; 
    buttonRecord.Enabled = true;
    buttonStop.Enabled = false;
    if (closing) 
    { 
        waveIn.Dispose();
    }
};
```

Now all our handlers are set up, we're ready to show the dialog:

``` c#
f.ShowDialog();
```

Here's the full program for reference:

``` c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var outputFilePath = Path.Combine(outputFolder,"recorded.wav");

var waveIn = new WaveInEvent();

WaveFileWriter writer = null;
bool closing = false;
var f = new Form();
var buttonRecord = new Button() { Text = "Record" };
var buttonStop = new Button() { Text = "Stop", Left = buttonRecord.Right, Enabled = false };
f.Controls.AddRange(new Control[] { buttonRecord, buttonStop });

buttonRecord.Click += (s, a) => 
{ 
    writer = new WaveFileWriter(outputFilePath, waveIn.WaveFormat); 
    waveIn.StartRecording(); 
    buttonRecord.Enabled = false; 
    buttonStop.Enabled = true; 
};

buttonStop.Click += (s, a) => waveIn.StopRecording();

waveIn.DataAvailable += (s, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
    if (writer.Position > waveIn.WaveFormat.AverageBytesPerSecond * 30)
    {
        waveIn.StopRecording();
    }
};

waveIn.RecordingStopped += (s, a) =>
{
    writer?.Dispose(); 
    writer = null; 
    buttonRecord.Enabled = true;
    buttonStop.Enabled = false;
    if (closing) 
    { 
        waveIn.Dispose();
    }
};

f.FormClosing += (s, a) => { closing=true; waveIn.StopRecording(); };
f.ShowDialog();
```

# Resampling Audio

Every now and then youll find you need to resample audio with NAudio.
For example, to mix files together of different sample rates, you need
to get them all to a common sample rate first. Or if youre playing audio
through an API like ASIO , audio must be resampled to match the output
device's current sample rate before being to the device.

There are also some gotchas you need to be aware of when resampling. In
particular there is the danger of "aliasing". I explain what this is in
my Pluralsight ["Digital Audio
Fundamentals"](https://www.shareasale.com/r.cfm?u=1036405&b=611266&m=53701&afftrack=&urllink=www%2Epluralsight%2Ecom%2Fcourses%2Fdigital%2Daudio%2Dfundamentals)
course. The main takeaway is that if you lower the sample rate, you
really ought to use a low pass filter first, to get rid of high
frequencies that cannot correctly.

### Option 1: MediaFoundationResampler

Probably the most powerful resampler available with NAudio is the
`MediaFoundationResampler`. This is not available for XP users, but
desktop versions of Windows from Vista onwards include it. If you are
using a Windows Server, youll need to make sure the "desktop experience"
is installed. It has a customisable quality level (60 is the highest
quality, down to 1 which is linear interpolation). Ive found its fast
enough to run on top quality. It also is quite flexible and is often
able to change to a different channel count or bit depth at the same
time.

Here's a code sample that resamples an MP3 file (usually 44.1kHz) down
to 16kHz. `The MediaFoundationResampler` takes an `IWaveProvider` as
input, and a desired output `WaveFormat`:

``` c#
int outRate = 16000;
var inFile = @"test.mp3";
var outFile = @"test resampled MF.wav";
using (var reader = new Mp3FileReader(inFile))
{
    var outFormat = new WaveFormat(outRate,    reader.WaveFormat.Channels);
    using (var resampler = new MediaFoundationResampler(reader, outFormat))
    {
        // resampler.ResamplerQuality = 60;
        WaveFileWriter.CreateWaveFile(outFile, resampler);
    }
}
```

### Option 2: WdlResamplingSampleProvider

The second option is based on the Cockos WDL resampler for which we were
kindly granted permission to use as part of NAudio. It works with
floating point samples, so you'll need an `ISampleProvider` to pass in.
Here we use `AudioFileReader` to get to floating point and then make a
resampled 16 bit WAV file:

``` c#
int outRate = 16000;
var inFile = @"test.mp3";
var outFile = @"test resampled WDL.wav";
using (var reader = new AudioFileReader(inFile))
{
    var resampler = new WdlResamplingSampleProvider(reader, outRate);
    WaveFileWriter.CreateWaveFile16(outFile, resampler);
}
```

The big advantage that the WDL resampler brings to the table is that it
is fully managed. This means it can be used within UWP Windows Store
apps (as Im still finding it very difficult to work out how to create
the `MediaFoundationResampler` in a way that passes WACK), or in
cross-platform scenarios.

The disadvantage is of course that performance will not necessarily be
as fast as using `MediaFoundationResampler`.

### Option 3: ACM Resampler

You can also use `WaveFormatConversionStream` which is an ACM based
Resampler, which has been in NAudio since the beginning and works back
to Windows XP. It resamples 16 bit only and you cant change the channel
count at the same time. It predates `IWaveProvider` so you need to pass
in a `WaveStream` based. Heres it being used to resample an MP3 file:

``` c#
int outRate = 16000;
var inFile = @"test.mp3";
var outFile = @"test resampled ACM.wav";
using (var reader = new Mp3FileReader(inFile))
{
    var outFormat = new WaveFormat(outRate,    reader.WaveFormat.Channels);
    using (var resampler = new WaveFormatConversionStream(outFormat, reader))
    {
        WaveFileWriter.CreateWaveFile(outFile, resampler);
    }
}
```

### Option 4: Do it yourself

Of course the fact that NAudio lets you have raw access to the samples
means you are able to write your own resampling algorithm, which could
be Linear Interpolation or something more complex. Id recommend against
doing this unless you really understand audio DSP. If you want to see
some spectograms showing what happens when you write your own naive
resampling algorithm, have a look at [this
article](http://www.codeproject.com/Articles/501521/How-to-convert-between-most-audio-formats-in-NET)
I wrote on CodeProject. Basically, youre likely to end up with
significant aliasing if you dont also write a low pass filter. Given
NAudio now has the WDL resampler, that should probably be used for all
cases where you need a fully managed resampler.

# Pitch Shifting with SmbPitchShiftingSampleProvider

The `SmbPitchShiftingSampleProvider` class provides a fully managed
pitch shifter effect.

You pass in the source audio to the constructor, and then use the
`PitchFactor` to set the amount of pitch shift. 1.0f means no pitch
change, 2.0f means an octave up, and 0.5f means an octave down. To move
up one semitone, use the twelfth root of two.

In this simple example, we calculate pitch factors to transpose an audio
file up and down a whole tone (two semitones). This demo just plays the
first 10 seconds of the audio file.

Note that pitch shifting algorithms do introduce artifacts. It may sound
slightly metalic, and the bigger the shift the bigger the effect. But
for practicing along to a backing track that's in the wrong key, this
can be a great benefit.

``` c#
var inPath = @"C:\Users\markh\example.mp3";
var semitone = Math.Pow(2, 1.0/12);
var upOneTone = semitone * semitone;
var downOneTone = 1.0/upOneTone;
using (var reader = new MediaFoundationReader(inPath))
{
    var pitch = new SmbPitchShiftingSampleProvider(reader.ToSampleProvider());
    using(var device = new WaveOutEvent())
    {
        pitch.PitchFactor = (float)upOneTone; // or downOneTone
        // just playing the first 10 seconds of the file
        device.Init(pitch.Take(TimeSpan.FromSeconds(10)));
        device.Play();
        while(device.PlaybackState == PlaybackState.Playing)
        {
            Thread.Sleep(500);
        }
    }
}
```

For an alternative approach to pitch shifting, look at creating a
managed wrapper for the SoundTouch library, as explained in [this
article](http://markheath.net/post/varispeed-naudio-soundtouch)

# Record Soundcard Output with WasapiLoopbackCapture

Lots of people ask how they can use NAudio to record the audio being
played by another program. The answer is that unfortunately Windows does
not provide an API that lets you target the output of one specific
program to record. However, with WASAPI loopback capture, you can record
all the audio that is being played out of a specific output device.

Since NAudio 2.1.0, the audio can be captured at a sample rate of your
choosing, although it will make sense to match the sound card's format.

Let's start off by selecting a path to record to, creating an instance
of `WasapiLoopbackCapture` (uses the default system device, but we can
pass any rendering `MMDevice` that we want which we can find with
`MMDeviceEnumerator`). We'll also create a `WaveFileWriter` using the
capture `WaveFormat`.

``` c#
var outputFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "NAudio");
Directory.CreateDirectory(outputFolder);
var outputFilePath = Path.Combine(outputFolder, "recorded.wav");
var capture = new WasapiLoopbackCapture();
// optionally we can set the capture waveformat here: e.g. capture.WaveFormat = new WaveFormat(44100, 16,2);
var writer = new WaveFileWriter(outputFilePath, capture.WaveFormat);
```

We need to handle the `DataAvailable` event, and it's very much the same
approach here as recording to a WAV file from a regular `WaveIn` device.
We just write `BytesRecorded` bytes from the `Buffer` into the
`WaveFileWriter`. And in this example, I am stopping recording when
we've captured 20 seconds worth of audio, by calling `StopRecording`.

``` c#
capture.DataAvailable += (s, a) =>
{
    writer.Write(a.Buffer, 0, a.BytesRecorded);
    if (writer.Position > capture.WaveFormat.AverageBytesPerSecond * 20)
    {
        capture.StopRecording();
    }
};
```

When the `RecordingStopped` event fires, we `Dispose` our
`WaveFileWriter` so we create a valid WAV file, and we're done recording
so we'll `Dispose` our capture device as well.

``` c#
capture.RecordingStopped += (s, a) =>
{
    writer.Dispose();
    writer = null;
    capture.Dispose();
};
```

All that remains is for us to start recording with `StartRecording` and
wait for recording to finish by monitoring the `CaptureState`.

``` c#
capture.StartRecording();
while (capture.CaptureState != NAudio.CoreAudioApi.CaptureState.Stopped)
{
    Thread.Sleep(500);
}
```

Now there is one gotcha with `WasapiLoopbackCapture`. If no audio is
playing whatsoever, then the `DataAvailable` event won't fire. So if you
want to record "silence", one simple trick is to simply use an NAudio
playback device to play silence through that device for the duration of
time you're recording. Alternatively, you could insert silence yourself
when you detect gaps in the incoming audio.

# Working with WasapiOut

`WasapiOut` is an implementation of `IWavePlayer` that uses the WASAPI
audio API under the hood. WASAPI was introduced with Windows Vista,
meaning it will be supported on most versions of Windows, but not XP.

## Configuring WasapiOut

When you create an instance of `WasapiOut` you can choose an output
device. This is discussed in the [enumerating output devices
article](EnumerateOutputDevices.md).

There are a number of other options you can specify with WASAPI.

First of all, you can choose the "share mode". This is normally set to
`AudioClientShareMode.Shared` which means you are happy to share the
sound card with other audio applications in Windows. This however does
mean that the sound card will continue to operate at whatever sample
rate it is currently set to, irrespective of the sample rate of audio
you want to play. Fortunately, since NAudio 2.1.0 `WasapiOut` in shared
mode will automatically resample the incoming audio.

If you choose `AudioClientShareMode.Exclusive` then you are requesting
exclusive access to the sound card. The benefits of this approach are
that you can specify the exact sample rate you want (has to be supported
by the sound card and usually cannot be less than 44.1kHz), and you can
often work at lower latencies. Obviously this mode impacts on other
programs wanting to use the soundcard.

You can choose whether to use `eventSync` or not. This governs the
behaviour of the background thread that is supplying audio to WASAPI.
With event sync, you listen on an event for when WASAPI wants more
audio. Without, you simply sleep for a short period of time and then
provide more audio. Event sync is the default and generally is fine for
most use cases.

You can also request the latency you want. This is only a request, and
depending on the share mode may not have any effect. The lower the
latency, the shorter the period of time between supplying audio to the
soundcard and hearing it. This can be very useful for real-time
monitoring effects, but comes at the cost of higher CPU usage and
potential for dropouts causing pops and clicks. So take care when
adjusting this setting. The default is currently set to a fairly
conservative 200ms.

## Playing Audio with WasapiOut

Once you've created an instance of `WasapiOut`, you use it exactly the
same as any other `IWavePlayer` device in NAudio. You call `Init` to
pass it the audio to be played, `Stop` to stop playback. You can use the
`Volume` property to adjust the volume and subscribe to
`PlaybackStopped` to determine when playback has stopped. And you should
call `Dispose` when you are finished with it.

Here's a simple example of playing audio with the default `WasapiOut`
device in shared mode with event sync and the default latency:

``` c#
using(var audioFile = new AudioFileReader(audioFile))
using(var outputDevice = new WasapiOut())
{
    outputDevice.Init(audioFile);
    outputDevice.Play();
    while (outputDevice.PlaybackState == PlaybackState.Playing)
    {
        Thread.Sleep(1000);
    }
}
```

# Render an Audio Wave Form to PNG

NAudio does not include any visualization code in the core library, but
it does provide access to the raw audio samples which you need to render
wave-forms.

NAudio does however provide a sample project at GitHub:
[NAudio.WaveFormRenderer](https://github.com/naudio/NAudio.WaveFormRenderer)
which makes use of `NAudio` and `System.Drawing` to render waveforms in
a variety of styles.

![Orange
Blocks](https://cloud.githubusercontent.com/assets/147668/18606778/5a9516ac-7cb1-11e6-8660-a0a80d72fe26.png)

\## WaveFormRendererLib

The `WaveFormRendererLib` project contains a customizable waveform
rendering algorithm, allowing you to

The waveform rendering algorithm is customizable:

- Supports several peak calculation strategies (max, average, sampled,
  RMS, decibels)
- Supports different colors or gradients for the top and bottom half
- Supports different sizes for top and bottom half
- Overall image size and background can be customized
- Transparent backgrounds
- Support for SoundCloud style bars
- Several built-in rendering styles

## WaveFormRenderer

The `WaveFormRenderer` class allows easy rendering of files. We need to
create some configuration options first.

The peak provider decides how peaks are calculated. There are four built
in options you can choose from. `MaxPeakProvider` simply picks out the
maximum sample value in the timeblock that each bar represents.
`RmsPeakProvider` calculates the root mean square of each sample and
returns the maximum value found in a specified blcok. The
`SamplingPeakProvider` simply samples the samples, and you pass in a
sample interval.Finally the `AveragePeakProvider` averages the sample
values and takes a scale parameter to multiply the average by as it
tends to produce lower values.

``` c#
var maxPeakProvider = new MaxPeakProvider();
var rmsPeakProvider = new RmsPeakProvider(blockSize); // e.g. 200
var samplingPeakProvider = new SamplingPeakProvider(sampleInterval); // e.g. 200
var averagePeakProvider = new AveragePeakProvider(scaleFactor); // e.g. 4
```

Next we need to provide the rendering settings. This is an instance of
`WaveFormRendererSettings` which specifies:

- **Width** - the width of the rendered image in pixels
- **TopHeight** - height of the top half of the waveform in pixels
- **BottomHeight** - height of the bottom half of the waveform in
  pixels. Normally set to the same as `TopHeight` but can be 0 or
  smaller for asymmetric waveforms
- **PixelsPerPeak** - allows for wider bars to represent each peak.
  Usually set to 1.
- **SpacerPixels** - allows blank spaces to be inserted between vertical
  bars. Usually 0 unless when wide bars are used.
- **TopPeakPen** - Pen to draw the top bars with
- **TopSpacerPen** - Pen to draw the top spacer bars with
- **BottomPeakPen** - Pen to draw the bottom bars with
- **BottomSpacerPen** - Pen to draw the bottom spacer bars with
- **DecibelScale** - if true, convert values to decibels for a
  logarithmic waveform
- **BackgroundColor** - background color (used if no `BackgroundImage`
  is specified)
- **BackgroundImage** - background image (alternative to solid color)

To simplify setting up an instance of `WaveFormRendererSettings` several
derived types are supplied including `StandardWaveFormRendererSettings`,
`SoundCloudOriginalSettings` and `SoundCloudBlockWaveFormSettings`. The
latter two mimic rendering styles that have been used by SoundCloud in
the past.

``` c#
var myRendererSettings = new StandardWaveFormRendererSettings();
myRendererSettings.Width = 640;
myRendererSettings.TopHeight = 32;
myRendererSettings.BottomHeight = 32;
```

Now we just need to create our `WaveFormRenderer` and give it a path to
the file we want to render, and pass in the peak provider we've chosen
and the renderer settings:

``` c#
var renderer = new WaveFormRenderer();
var audioFilePath = "myfile.mp3";
var image = renderer.Render(audioFilePath, myPeakProvider, myRendererSettings);
```

With that image we could render it to a WinForms picturebox:

``` c#
pictureBox1.Image = image;
```

Or we could save it to a PNG file which you'd want to do if you were
rendering on a web server for example:

``` c#
image.Save("myfile.png", ImageFormat.Png);
```

# WaveStream, IWaveProvider and ISampleProvider

When you play audio with NAudio or construct a playback graph, you are
typically working with either `IWaveProvider` or `ISampleProvider`
interface implementations. This article explains the three main base
interfaces and classes you will encounter in NAudio and when you might
use them.

## WaveStream

`WaveStream` was the first base class in NAudio, and inherits from
`System.IO.Stream`. It represents a stream of audio data, and its format
can be determined by looking at the `WaveFormat` property.

It supports reporting `Length` and `Position` and these are both
measured in terms of bytes, not samples. `WaveStreams` can be
repositioned (assuming the underlying implementation supports that),
although care must often be taken to reposition to a multiple of the
`BlockAlign` of the `WaveFormat`. For example if the wave stream
produces 16 bit samples, you should always reposition to an even
numbered byte position.

Audio data is from a stream using the `Read` method which has the
signature:

``` c#
int Read(byte[] destBuffer, int offset, int numBytes)
```

This method is inherited from `System.IO.Stream`, and works in the
standard way. The `destBuffer` is the buffer into which audio should be
written. The `offset` parameter specifies where in the buffer to write
audio to (this parameter is almost always 0), and the `numBytes`
parameter is how many bytes of audio should be read.

The `Read` method returns the number for bytes that were read. This
should never be more than `numBytes` and can only be less if the end of
the audio stream is reached. NAudio playback devices will stop playing
when `Read` returns 0.

`WaveStream` is the base class for NAudio file reader classes such as
`WaveFileReader`, `Mp3FileReader`, `AiffFileReader` and
`MediaFoundationReader`. It is a good choice of base class because these
inherently support repositioning. `RawSourceWaveStream` is also a
`WaveStream`, and delegates repositioning requests down to its source
stream.

For a more detailed look at all the methods on `WaveStream`, see [this
article](http://markheath.net/post/naudio-wavestream-in-depth)

## IWaveProvider

Implementing `WaveStream` can be quite a lot of work, and for
non-repositionable streams can seem like overkill. Also, streams that
simply read from a source and modify or analyse audio as it passes
through don't really benefit from inheriting from `WaveStream`.

So the `IWaveProvider` interface provides a much simpler, minimal
interface that simply has the `Read` method, and a `WaveFormat`
property.

``` c#
public interface IWaveProvider
{
    WaveFormat WaveFormat { get; }
    int Read(byte[] buffer, int offset, int count);
}
```

The `IWavePlayer` interface only needs an `IWaveProvider` passed to its
`Init` method in order to be able to play audio.
`WaveFileWriter.CreateWaveFile` and `MediaFoundationEncoder.EncodeToMp3`
also only needs an `IWaveProvider` to dump the audio out to a WAV file.
So in many cases you won't need to create a `WaveStream` implementation,
just implement `IWaveProvider` and you've got an audio source that can
be played or rendered to a file.

`BufferedWaveProvider` is a good example of a `IWaveProvider` as it has
no ability to reposition - it simply returns any buffered audio from its
`Read` method.

## ISampleProvider

The strength of `IWaveProvider` is that it can be used to represent
audio in any format. It can be used for 16,24 or 32 bit PCM audio, and
even for compressed audio (MP3, G.711 etc). But if you are performing
any kind of signal processing or analysis on the audio, it is very
likely that you want the audio to be in 32 bit IEEE floating point
format. And it can be a pain to try to read floating point values out of
a `byte[]` in C#.

So `ISampleProvider` defines an interface where the samples are all 32
bit floating point:

``` c#
public interface ISampleProvider
{
    WaveFormat WaveFormat { get; }
    int Read(float[] buffer, int offset, int count);
}
```

The `WaveFormat` will always be 32 bit floating point, but the number of
channels or sample rate may of course vary.

The `Read` method's `count` parameter specifies the number of samples to
be read, and the method returns the number of samples written into
`buffer`.

`ISampleProvider` is a great base interface to inherit from if you are
implementing any kind of audio effects. In the `Read` method you
typically read from your source `ISampleProvider`, then modify the
floating point samples, before returning them. Here's the implementation
of the `Read` method in `VolumeSampleProvider` showing how simple this
can be:

``` c#
public int Read(float[] buffer, int offset, int sampleCount)
{
    int samplesRead = source.Read(buffer, offset, sampleCount);
    if (volume != 1f)
    {
        for (int n = 0; n < sampleCount; n++)
        {
            buffer[offset + n] *= volume;
        }
    }
    return samplesRead;
}
```

NAudio makes it easy to go from an `IWaveProvider` to an
`ISampleProvider` with the `ToSampleProvider` extension method. You can
also use `AudioFileReader` which reads a wide variety of file types and
implements `ISampleProvider`.

You can get back to an `IWaveProvider` with the `ToWaveProvider`
extension method. Or there's the `ToWaveProvider16` extension method if
you want to go back to 16 bit integer samples.
