using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Backend.Helper;

public interface ITError
{
    string Message { get; }
    int Code { get; }
    string FilePath { get; }
    string Function { get; }

    bool IsNotFound();
    bool IsClient();
    bool IsServer();
    bool IsNotAllowed();
    Exception ToException();
}

public class TError : Exception, ITError
{
    [JsonPropertyName("code")]
    public int Code { get; private set; }
    [JsonPropertyName("file_path")]
    public string FilePath { get; private set; }
    [JsonPropertyName("function")]
    public string Function { get; private set; }

    public TError(int code, string message, string filePath, string function)
        : base(message)
    {
        Code = code;
        FilePath = filePath;
        Function = function;
    }

    // === Factory Methods (similar to NewServer/NewClient/... in Go) ===

    public static TError NewServer(params object[] msg)
        => New(HttpStatusCodes.InternalServerError, msg);

    public static TError NewClient(params object[] msg)
        => New(HttpStatusCodes.BadRequest, msg);

    public static TError NewNotFound(params object[] msg)
        => New(HttpStatusCodes.NotFound, msg);

    public static TError NewNotAllowed(params object[] msg)
        => New(HttpStatusCodes.Forbidden, msg);

    // === Shared builder ===
    private static TError New(int code, params object[] msg)
    {
        var message = ChainString(msg);
        var (file, func) = GetCallerInfo();
        return new TError(code, message, file, func);
    }

    // === Behavior match with Go ===
    public bool IsServer() => Code == HttpStatusCodes.InternalServerError;
    public bool IsClient() => Code == HttpStatusCodes.BadRequest;
    public bool IsNotFound() => Code == HttpStatusCodes.NotFound;
    public bool IsNotAllowed() => Code == HttpStatusCodes.Forbidden;

    public Exception ToException() => new Exception(Message.ToLowerInvariant());

    public override string ToString()
        => $"[{Function}] {Message} at {FilePath}";

    // === Helper functions ===
    private static (string File, string Func) GetCallerInfo()
    {
        var st = new StackTrace(true);
        if (st.FrameCount < 3) return ("", "");
        var frame = st.GetFrame(3);
        var method = frame?.GetMethod();

        var file = frame?.GetFileName() ?? "";
        var line = frame?.GetFileLineNumber() ?? 0;

        // If compiler-generated async state machine, extract the original method
        var func = method?.Name ?? "unknown";
        var declaringType = method?.DeclaringType;
        if (declaringType != null && func == "MoveNext")
        {
            var realMethod = declaringType.DeclaringType?
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .FirstOrDefault(m => m.GetCustomAttributes(typeof(AsyncStateMachineAttribute), false)
                    .FirstOrDefault() is AsyncStateMachineAttribute attr &&
                    attr.StateMachineType == declaringType);

            if (realMethod != null)
            {
                func = realMethod.Name;
            }
        }

        return ($"{file.Replace('\\', '/')}:{line}", func);
    }

    private static string ChainString(params object[] msg)
    {
        foreach (var v in msg)
        {
            switch (v)
            {
                case string s when !string.IsNullOrWhiteSpace(s):
                    return s;
                case TError terr when !string.IsNullOrWhiteSpace(terr.Message):
                    return terr.Message;
                case Exception ex when !string.IsNullOrWhiteSpace(ex.Message):
                    return ex.Message;
            }
        }
        return "unknown error";
    }
}

// === simple static HttpStatusCodes helper ===
public static class HttpStatusCodes
{
    public const int BadRequest = 400;
    public const int Forbidden = 403;
    public const int NotFound = 404;
    public const int InternalServerError = 500;
}
