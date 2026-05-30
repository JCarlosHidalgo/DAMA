namespace Backend.Services.Abstract;

public interface ICallbackSignature
{
    string Sign(string payload);

    bool Verify(string payload, string providedSignature);
}
