namespace ViciOne.Driver.IoTCore.BlobRecording;

internal enum BlobCommand : byte
{
    BLOB_Abort = 0xF0,
    BLOB_Start = 0xF1,
    BLOB_Finish = 0xF2
}
