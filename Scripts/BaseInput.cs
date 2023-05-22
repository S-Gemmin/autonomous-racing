public struct InputData
{
    public bool accelerate;
    public bool brake;
    public float turnInput;
}

public interface IInput
{
    InputData GenerateInput();
}