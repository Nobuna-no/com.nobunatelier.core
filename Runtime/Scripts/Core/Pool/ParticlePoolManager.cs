using NobunAtelier;

public class ParticlePoolManager : PoolManager<ParticlePoolManager>
{
    public static ParticlePoolManager Instance { get; private set; }

    private void Awake()
    {
        Instance = this;
    }
}