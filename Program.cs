using Sres.Net.EEIP;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace KeyenceTest
{
  class Program
  {
    static int INIT_TIME = 5000;
    static int INIT_SLEEP = 500;
    static int TRIGGER_TIME = 5000;
    static int TRIGGER_SLEEP = 100;
    static int PROGRAM_TIME = 2500;
    static int PROGRAM_SLEEP = 100;

    static bool debug = false;
    static int repeat = 0;
    static string host = "10.13.37.150";
    static ushort port = 44818;
    static ushort program = 0;
    static byte tools = 0;

    static bool cancelled = false;
    static EEIPClient client;
    static InputAssembly result;

    static void ParseArgs(string[] args)
    {
      for (var i = 0; i < args.Length; ++i)
      {
        var k = args[i];

        switch (k)
        {
          case "--debug":
            debug = args[++i] == "1";
            break;

          case "--repeat":
            repeat = int.Parse(args[++i]);

            if (repeat < 0)
            {
              throw new Exception("--repeat must be greater than or equal to 0.");
            }
            break;

          case "--host":
            host = args[++i];
            break;

          case "--port":
            port = ushort.Parse(args[++i]);

            if (port <= 0 || port > 65535)
            {
              throw new Exception("--port must be between 1 and 65535.");
            }
            break;

          case "--program":
            program = ushort.Parse(args[++i]);

            if (program < 0 || program > 31)
            {
              throw new Exception("--program must be between 0 and 31.");
            }
            break;

          case "--tools":
            tools = byte.Parse(args[++i]);

            if (tools < 0 || tools > 16)
            {
              throw new Exception("--tools must be between 0 and 16.");
            }
            break;
        }
      }
    }

    static void Main(string[] args)
    {
      try
      {
        ParseArgs(args);
      }
      catch (Exception x)
      {
        Console.Error.WriteLine("Failed to parse arguments: {0}", x.Message);
        Console.Error.WriteLine("ERR_INVALID_ARGS");
        Environment.Exit(1);
      }

      Console.CancelKeyPress += Console_CancelKeyPress;

      client = new EEIPClient()
      {
        IPAddress = host,
        TCPPort = port
      };

      Console.Error.WriteLine($"Connecting to {host}:{port}...");

      try
      {
        client.RegisterSession();
      }
      catch (Exception x)
      {
        Console.Error.WriteLine("Failed to connect: {0}", x.Message);
        Console.Error.WriteLine("ERR_CONNECTION_FAILURE");
        Environment.Exit(1);
      }

      while (!IsCancelled())
      {
        try
        {
          Run();
        }
        catch (Exception x)
        {
          if (IsCancelled())
          {
            Console.Error.WriteLine("ERR_CANCELLED");
          }
          else if (x.Message.StartsWith("ERR_"))
          {
            Console.Error.WriteLine(x.Message);
          }
          else
          {
            Console.Error.WriteLine(x.Message);
            Console.Error.WriteLine("ERR_EXCEPTION");
          }

          if (repeat == 0)
          {
            Environment.Exit(1);
          }
        }

        if (repeat == 0)
        {
          break;
        }

        if (!IsCancelled())
        {
          Thread.Sleep(repeat);
        }

        Console.Error.WriteLine();
      }

      try
      {
        Console.Error.WriteLine("Closing the connection...");
        
        client.UnRegisterSession();
        client = null;
      }
      catch (Exception) { }

      if (result != null)
      {
        var json = new StringBuilder();

        json.Append("{");
        json.Append(@"""program"":");
        json.Append(result.ProgramNoDuringJudgement);
        json.Append(@",""result"":");
        json.Append(result.OverallJudgement ? "true" : "false");
        json.Append(@",""processingTime"":");
        json.Append(result.ProcessingTime);

        if (tools > 0)
        {
          json.Append(@",""tools"":[");

          for (var no = 1; no <= tools; ++no)
          {
            var tool = result.Tools[no - 1];

            json.Append("{");
            json.Append(@"""result"":");
            json.Append(tool.Result ? "true" : "false");
            json.Append(@",""matchingRate"":");
            json.Append(tool.MatchingRate);
            json.Append(@",""lowerThreshold"":");
            json.Append(tool.LowerThreshold);
            json.Append(@",""upperThreshold"":");
            json.Append(tool.UpperThreshold);
            json.Append("}");

            if (tool.No < tools)
            {
              json.Append(",");
            }
          }

          json.Append("]");
        }

        json.Append("}");

        Console.WriteLine(json.ToString());
      }
    }

    private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
    {
      e.Cancel = true;
      cancelled = true;
    }

    private static bool IsCancelled()
    {
      return cancelled;
    }

    private static void Run()
    {
      ResetState();
      CheckInitialConditions();
      SelectProgram();
      Trigger();
      ResetState();
    }

    private static void ResetState()
    {
      Console.Error.WriteLine("Resetting state...");

      WriteRequest(new OutputAssembly()
      {
        WarningClearRequest = true,
        StatisticsResetRequest = true,
        BufferClearRequest = true
      });
    }

    private static void CheckInitialConditions()
    {
      Console.Error.WriteLine("Checking initial conditions...");

      var startedAt = DateTime.Now;
      InputAssembly res;

      while (!IsCancelled() && DateTime.Now.Subtract(startedAt).TotalMilliseconds <= INIT_TIME)
      {
        res = ReadResponse();

        CheckResponse(res);

        if (res.Run && res.Ready)
        {
          break;
        }

        Console.Error.WriteLine("...not Running and Ready...");

        Thread.Sleep(INIT_SLEEP);
      }

      if (IsCancelled())
      {
        throw new Exception();
      }

      res = ReadResponse();

      if (res.Run && res.Ready)
      {
        Console.Error.WriteLine("...Running and Ready!");
      }
      else
      {
        if (!res.Run && !res.Ready)
        {
          Console.Error.WriteLine("...still not Running and Ready!");
        }
        else if (!res.Run)
        {
          Console.Error.WriteLine("...still not Running!");
        }
        else if (!res.Ready)
        {
          Console.Error.WriteLine("...still not Ready!");
        }

        throw new Exception("ERR_INVALID_INITIAL_CONDITIONS");
      }
    }

    private static void SelectProgram()
    {
      Console.Error.WriteLine($"Selecting program no. {program}...");

      if (ReadResponse().CurrentProgramNo == program)
      {
        Console.Error.WriteLine("Program already selected.");

        return;
      }

      WriteRequest(new OutputAssembly() { ProgramSwitchingRequest = true, ProgramNo = program });

      var startedAt = DateTime.Now;

      while (!IsCancelled() && DateTime.Now.Subtract(startedAt).TotalMilliseconds <= PROGRAM_TIME)
      {
        var res = ReadResponse();

        CheckResponse(res);

        if (!res.ProgramSwitchingResponse)
        {
          Console.Error.WriteLine("...program not selected yet...");

          Thread.Sleep(PROGRAM_SLEEP);

          continue;
        }

        if (res.ProgramSwitchingFailed)
        {
          break;
        }

        Console.Error.WriteLine("...program selected.");

        return;
      }

      throw new Exception("ERR_PROGRAM_SELECTION_FAILED");
    }

    private static void Trigger()
    {
      var oldResultUpdateComplete = ReadResponse().ResultUpdateComplete;

      Console.Error.WriteLine("Result update complete before trigger is {0}.", oldResultUpdateComplete ? 1 : 0);

      Console.Error.WriteLine("Triggering...");

      WriteRequest(new OutputAssembly() { TriggerRequest = true });

      var startedAt = DateTime.Now;

      while (!IsCancelled() && DateTime.Now.Subtract(startedAt).TotalMilliseconds <= TRIGGER_TIME)
      {
        var res = ReadResponse();

        CheckResponse(res);

        if (!res.TriggerResponse || !res.ResultAvailable || res.ResultUpdateComplete == oldResultUpdateComplete)
        {
          result = null;

          Console.Error.WriteLine("...result not available...");

          Thread.Sleep(TRIGGER_SLEEP);

          continue;
        }

        result = res;

        Console.Error.WriteLine("...result available!");

        return;
      }

      throw new Exception("ERR_RESULT_NOT_AVAILABLE");
    }

    private static void CheckResponse(InputAssembly res)
    {
      if (res.ErrorStatus && res.ErrorCode > 0)
      {
        CheckResponseErrors(res.ErrorCode);
      }

      if (res.WarningStatus && res.WarningCode > 0)
      {
        CheckResponseWarnings(res.WarningCode);
      }
    }

    private static void CheckResponseErrors(ushort code)
    {
      string error = "ERROR";

      switch (code)
      {
        case 75: error = "EEPROM"; break;
        case 76: error = "FLASHROM"; break;
        default:
          if (code >= 1 && code <= 32) error = "PROGRAM_CORRUPTION";
          break;
      }

      throw new Exception($"ERR_KEYENCE_{error}={code}");
    }

    private static void CheckResponseWarnings(ushort code)
    {
      string error = "WARNING";

      switch (code)
      {
        case 58: error = "EXTERNAL_MASTER_REGISTRATION"; break;
        case 59: error = "SYNC"; break;
        case 60: error = "FIELD_NETWORK_OCR"; break;
        case 61: error = "FIELD_NETWORK_THRESHOLD"; break;
        case 62: error = "FIELD_NETWORK_OVERRUN"; break;
        case 63: error = "FIELD_NETWORK_MASTER_REGISTRATION"; break;
        case 64: error = "FIELD_NETWORK_PROGRAM_SWITCHING"; break;
        case 65: error = "TRIGGER"; break;
        case 66: error = "EXTERNAL_MASTER_OUTLINE"; break;
        case 67: error = "EXTERNAL_MASTER_AREA"; break;
        case 68: error = "EXTERNAL_MASTER_BRIGHTNESS_CORRECTION"; break;
        case 69: error = "EXTERNAL_MASTER_EDGE"; break;
        case 70: error = "FTP_BUFFER"; break;
        case 71: error = "FTP_TRANSFER"; break;
        case 72: error = "FTP_CONNECTION"; break;
        case 73: error = "EXTERNAL_MASTER_WORK_MEMORY"; break;
        case 74: error = "EXTERNAL_MASTER_NO_IMAGES"; break;
      }

      throw new Exception($"ERR_KEYENCE_{error}={code}");
    }

    private static void WriteRequest(OutputAssembly request, int sleepMs = 30)
    {
      client.AssemblyObject.setInstance(101, request.ToBuffer());

      if (sleepMs > 0)
      {
        Thread.Sleep(sleepMs);
      }
    }

    private static InputAssembly ReadResponse()
    {
      var response = new InputAssembly(client.AssemblyObject.getInstance(100));

      if (debug)
      {
        LogResponse(response);
      }

      return response;
    }

    private static void LogResponse(InputAssembly response)
    {
      Console.Write("Response =");
      if (response.TriggerResponse) Console.Write(" Trigger");
      if (response.MasterImageRegistrationResponse) Console.Write(" MasterImageRegistration");
      if (response.ProgramSwitchingResponse) Console.Write(" ProgramSwitching");
      if (response.WarningClearResponse) Console.Write(" WarningClear");
      if (response.StatisticsResetResponse) Console.Write(" StatisticsReset");
      if (response.BufferClearResponse) Console.Write(" BufferClear");
      if (response.SettingValueChangeResponse) Console.Write(" SettingValueChange");
      Console.WriteLine();

      Console.Write("Failed =");
      if (response.TriggerFailed) Console.Write(" Trigger");
      if (response.MasterImageRegistrationFailed) Console.Write(" MasterImageRegistration");
      if (response.ProgramSwitchingFailed) Console.Write(" ProgramSwitching");
      if (response.SettingValueChangeFailed) Console.Write(" SettingValueChange");
      Console.WriteLine();

      Console.Write("Status =");
      if (response.ResultAvailable) Console.Write(" ResultAvailable");
      if (response.ResultUpdateComplete) Console.Write(" ResultUpdateComplete");
      if (response.Busy) Console.Write(" Busy");
      if (response.Imaging) Console.Write(" Imaging");
      if (response.Run) Console.Write(" Run");
      if (response.Ready) Console.Write(" Ready");
      if (response.BufferOverrunStatus) Console.Write(" BufferOverrun");
      if (response.WarningStatus) Console.Write(" Warning");
      if (response.ErrorStatus) Console.Write(" Error");
      Console.WriteLine();

      /*
      Console.WriteLine("Information");
      if (response.ErrorCode > 0) Console.WriteLine($"\tErrorCode = {response.ErrorCode}");
      if (response.WarningCode > 0) Console.WriteLine($"\tWarningCode = {response.WarningCode}");
      Console.WriteLine($"\tCurrentProgramNo = {response.CurrentProgramNo}");
      Console.WriteLine($"\tProgramNoDuringJudgement = {response.ProgramNoDuringJudgement}");
      Console.Write($"\tResultNo={response.ResultNo}");
      Console.Write($"\tProcessingTime={response.ProcessingTime}");
      Console.WriteLine();

      Console.Write($"OverallJudgement = {response.OverallJudgement}");
      Console.WriteLine();
      Console.WriteLine();
      */
    }

    static void Discover()
    {
      Sres.Net.EEIP.EEIPClient eipClient = new Sres.Net.EEIP.EEIPClient();
      List<Sres.Net.EEIP.Encapsulation.CIPIdentityItem> cipIdentityItem = eipClient.ListIdentity();

      for (int i = 0; i < cipIdentityItem.Count; i++)
      {
        Console.WriteLine("Ethernet/IP Device Found:");
        Console.WriteLine(cipIdentityItem[i].ProductName1);
        Console.WriteLine("IP-Address: " + Sres.Net.EEIP.Encapsulation.CIPIdentityItem.getIPAddress(cipIdentityItem[i].SocketAddress.SIN_Address));
        Console.WriteLine("Port: " + cipIdentityItem[i].SocketAddress.SIN_port);
        Console.WriteLine("Vendor ID: " + cipIdentityItem[i].VendorID1);
        Console.WriteLine("Product-code: " + cipIdentityItem[i].ProductCode1);
        Console.WriteLine("Type-Code: " + cipIdentityItem[i].ItemTypeCode);
      }

      Console.WriteLine("Continue...");
      Console.ReadLine();
    }
  }
}
