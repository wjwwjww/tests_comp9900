public class Oxygen : Molecule
{
    private bool _isConsumed;

    public bool TryConsume()
    {
        if (_isConsumed) return false;

        _isConsumed = true;
        return true;
    }

    public override string GetState()
    {
        return _isConsumed ? "Consumed" : "Available";
    }
}
