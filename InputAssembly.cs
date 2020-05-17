using System;
using System.Collections.Generic;

namespace KeyenceTest
{
  public class InputAssembly
  {
    public byte[] Response { get; private set; }

    // Input Assembly Address 0: Control result (response)
    public bool TriggerResponse { get; private set; }
    public bool MasterImageRegistrationResponse { get; private set; }
    public bool ProgramSwitchingResponse { get; private set; }
    public bool WarningClearResponse { get; private set; }
    public bool StatisticsResetResponse { get; private set; }
    public bool BufferClearResponse { get; private set; }
    public bool SettingValueChangeResponse { get; private set; }

    // Input Assembly Address 1: Control error result
    public bool TriggerFailed { get; private set; }
    public bool MasterImageRegistrationFailed { get; private set; }
    public bool ProgramSwitchingFailed { get; private set; }
    public bool SettingValueChangeFailed { get; private set; }

    // Input Assembly Address 2 to 3: Handshake control/status/error result
    public bool ResultAvailable { get; private set; }
    public bool ResultUpdateComplete { get; private set; }
    public bool Busy { get; private set; }
    public bool Imaging { get; private set; }
    public bool Run { get; private set; }
    public bool Ready { get; private set; }
    public bool BufferOverrunStatus { get; private set; }
    public bool WarningStatus { get; private set; }
    public bool ErrorStatus { get; private set; }

    // Input Assembly Address 4 to 7: Status result
    public bool OverallJudgement { get; private set; }
    public bool PositionCorrection { get; private set; }
    public bool[] Logic { get; private set; }
    public bool OverallJudgementNg { get; private set; }

    // Input Assembly Address 8 to 23: Error/status/status result information
    public ushort ErrorCode { get; private set; }
    public ushort WarningCode { get; private set; }
    public ushort NumberOfRemainingBuffers { get; private set; }
    public ushort Checksum { get; private set; }
    public ushort CurrentProgramNo { get; private set; }
    public ushort ProgramNoDuringJudgement { get; private set; }
    public ushort ResultNo { get; private set; }
    public ushort ProcessingTime { get; private set; }

    // Input Assembly Address 24 to 51: Statistics information
    public ushort ProcessingTimeMax { get; private set; }
    public ushort ProcessingTimeMin { get; private set; }
    public ushort ProcessingTimeAvg { get; private set; }
    public uint NumberOfTriggers { get; private set; }
    public uint NumberOfOks { get; private set; }
    public uint NumberOfNgs { get; private set; }
    public uint NumberOfTriggerErrors { get; private set; }

    // Input Assembly Address 24 to 51: Statistics information
    public ushort PositionCorrectionToolMatchingRate { get; private set; }
    public ushort PositionCorrectionToolMatchingRateMax { get; private set; }
    public ushort PositionCorrectionToolMatchingRateMin { get; private set; }
    public ushort PositionCorrectionToolThreshold { get; private set; }

    public IList<Tool> Tools { get; private set; }

    public InputAssembly(byte[] response)
    {
      Response = response;

      ReadControlResultParams();
      ReadControlErrorResultParams();
      ReadHandshakeResultParams();
      ReadStatusResultParams();
      ReadResultInformationParams();
      ReadStatisticsInformationParams();
      ReadPositionCorrectionInformationParams();
      ReadToolParams();
    }

    private void ReadControlResultParams()
    {
      TriggerResponse = ReadBoolParam(0, 0);
      MasterImageRegistrationResponse = ReadBoolParam(0, 1);
      ProgramSwitchingResponse = ReadBoolParam(0, 2);
      WarningClearResponse = ReadBoolParam(0, 3);
      StatisticsResetResponse = ReadBoolParam(0, 4);
      BufferClearResponse = ReadBoolParam(0, 5);
      SettingValueChangeResponse = ReadBoolParam(0, 7);
    }

    private void ReadControlErrorResultParams()
    {
      TriggerFailed = ReadBoolParam(1, 0);
      MasterImageRegistrationFailed = ReadBoolParam(1, 1);
      ProgramSwitchingFailed = ReadBoolParam(1, 2);
      SettingValueChangeFailed = ReadBoolParam(1, 7);
    }

    private void ReadHandshakeResultParams()
    {
      ResultAvailable = ReadBoolParam(2, 0);
      ResultUpdateComplete = ReadBoolParam(2, 1);
      Busy = ReadBoolParam(2, 2);
      Imaging = ReadBoolParam(2, 3);
      Run = ReadBoolParam(2, 4);
      Ready = ReadBoolParam(2, 5);
      BufferOverrunStatus = ReadBoolParam(3, 5);
      WarningStatus = ReadBoolParam(3, 6);
      ErrorStatus = ReadBoolParam(3, 7);
    }

    private void ReadStatusResultParams()
    {
      OverallJudgement = ReadBoolParam(4, 0);
      PositionCorrection = ReadBoolParam(4, 1);
      Logic = new bool[]
      {
        ReadBoolParam(4, 2),
        ReadBoolParam(4, 3),
        ReadBoolParam(4, 4),
        ReadBoolParam(4, 5)
      };
      OverallJudgementNg = ReadBoolParam(4, 6);
    }

    private void ReadResultInformationParams()
    {
      ErrorCode = ReadUintParam(8);
      WarningCode = ReadUintParam(10);
      NumberOfRemainingBuffers = ReadUintParam(12);
      Checksum = ReadUintParam(14);
      CurrentProgramNo = ReadUintParam(16);
      ProgramNoDuringJudgement = ReadUintParam(18);
      ResultNo = ReadUintParam(20);
      ProcessingTime = ReadUintParam(22);
    }

    private void ReadStatisticsInformationParams()
    {
      ProcessingTimeMax = ReadUintParam(24);
      ProcessingTimeMin = ReadUintParam(26);
      ProcessingTimeAvg = ReadUintParam(28);
      NumberOfTriggers = ReadUintParam(32);
      NumberOfOks = ReadUintParam(36);
      NumberOfNgs = ReadUintParam(40);
      NumberOfTriggerErrors = ReadUintParam(44);
    }

    private void ReadPositionCorrectionInformationParams()
    {
      PositionCorrectionToolMatchingRate = ReadUintParam(52);
      PositionCorrectionToolMatchingRateMax = ReadUintParam(54);
      PositionCorrectionToolMatchingRateMin = ReadUintParam(56);
      PositionCorrectionToolThreshold = ReadUintParam(58);
    }

    private void ReadToolParams()
    {
      Tools = new List<Tool>(16);

      for (byte no = 1; no <= 16; ++no)
      {
        Tools.Add(new Tool(no, Response));
      }
    }

    private bool ReadBoolParam(int address, int bit)
    {
      return ReadBoolParam(Response[address], bit);
    }

    static bool ReadBoolParam(byte value, int bit)
    {
      return IsBitSet(value, bit);
    }

    private ushort ReadUintParam(int address)
    {
      return ReadUintParam(Response, address);
    }

    static ushort ReadUintParam(byte[] value, int address)
    {
      return BitConverter.ToUInt16(value, address);
    }

    private uint ReadUdintParam(int address)
    {
      return ReadUdintParam(Response, address);
    }

    static uint ReadUdintParam(byte[] value, int address)
    {
      return BitConverter.ToUInt32(value, address);
    }

    static bool IsBitSet(byte b, int pos)
    {
      return (b & (1 << pos)) != 0;
    }

    public class Tool
    {
      public byte No { get; private set; }
      public bool Result { get; private set; }
      public ushort MatchingRate { get; private set; }
      public ushort MatchingRateMax { get; private set; }
      public ushort MatchingRateMin { get; private set; }
      public ushort LowerThreshold { get; private set; }
      public ushort UpperThreshold { get; private set; }
      public ushort DecimalPointPosition { get; private set; }
      public ushort PitchPresentValueMax { get; private set; }
      public ushort PitchPresentValueMin { get; private set; }
      public ushort NumberOfPitches { get; private set; }

      public Tool(byte no, byte[] response)
      {
        No = no;
        Result = no > 8 ? IsBitSet(response[7], no - 9) : IsBitSet(response[6], no - 1);

        var addr = 72 + (no - 1) * 20;

        MatchingRate = ReadUintParam(response, addr);
        MatchingRateMax = ReadUintParam(response, addr + 2);
        MatchingRateMin = ReadUintParam(response, addr + 4);
        LowerThreshold = ReadUintParam(response, addr + 6);
        UpperThreshold = ReadUintParam(response, addr + 8);
        DecimalPointPosition = ReadUintParam(response, addr + 10);
        PitchPresentValueMax = ReadUintParam(response, addr + 12);
        PitchPresentValueMin = ReadUintParam(response, addr + 14);
        NumberOfPitches = ReadUintParam(response, addr + 16);
      }
    }
  }
}
