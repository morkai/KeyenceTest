using System.IO;

namespace KeyenceTest
{
  public class OutputAssembly
  {
    public bool TriggerRequest { get; set; } = false;
    public bool MasterRegistrationRequest { get; set; } = false;
    public bool ProgramSwitchingRequest { get; set; } = false;
    public bool WarningClearRequest { get; set; } = false;
    public bool StatisticsResetRequest { get; set; } = false;
    public bool BufferClearRequest { get; set; } = false;
    public bool SettingValueChangeRequest { get; set; } = false;
    public bool ResultAcquisitionCompleteNotification { get; set; } = false;
    public ushort ProgramNo { get; set; } = 0;
    public ushort SettingNo { get; set; } = 0;
    public uint SettingValue { get; set; } = 0;

    public OutputAssembly()
    {
      
    }

    public byte[] ToBuffer()
    {
      var stream = new MemoryStream(12);
      var writer = new BinaryWriter(stream);
      var requests = (byte)0;
      var reserved = (byte)0;

      if (TriggerRequest) requests |= 1;
      if (MasterRegistrationRequest) requests |= 2;
      if (ProgramSwitchingRequest) requests |= 4;
      if (WarningClearRequest) requests |= 8;
      if (StatisticsResetRequest) requests |= 16;
      if (BufferClearRequest) requests |= 32;
      if (SettingValueChangeRequest) requests |= 128;

      writer.Write(requests);
      writer.Write(reserved);
      writer.Write(ResultAcquisitionCompleteNotification);
      writer.Write(reserved);
      writer.Write(ProgramNo);
      writer.Write(SettingNo);
      writer.Write(SettingValue);

      return stream.ToArray();
    }
  }
}
