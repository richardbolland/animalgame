using UnityEngine;

[ExecuteAlways]
public class SandController : MonoBehaviour
{
    public ParticleSystem ForegroundSand;
    public ParticleSystem BackgroundSand;

    [Range(0,1)]
    public float Sandyness = 1;
    
    public float MaxForegroundSpeed;
    public float MaxBackgroundSpeed;
    public float MaxForegroundScale;
    public float MaxBackgroundScale;
    
    public float MaxForegroundEmission;
    public float MaxBackgroundEmission;

    public float C = 4000;

    public float InterpSpeed = 1f;

    public AnimationCurve Curve;

    private float _sandinessActual = 0;

    public bool FrontFootGrounded = false;
    public bool BackFootGrounded = false;

    // Update is called once per frame
    void Update()
    {
        var foregroundMain = ForegroundSand.main;
        var foregroundEmission = ForegroundSand.emission;
        
        var backgroundMain = BackgroundSand.main;
        var backgroundEmission = BackgroundSand.emission;

        _sandinessActual = Mathf.MoveTowards(_sandinessActual, Sandyness, Time.deltaTime * InterpSpeed);
        float t = Curve.Evaluate(_sandinessActual);

        if (FrontFootGrounded)
        {
            foregroundMain.startSpeedMultiplier = t * MaxForegroundSpeed;
            foregroundMain.startSizeMultiplier = t * MaxForegroundScale;
            foregroundEmission.rateOverTimeMultiplier = t * MaxForegroundEmission;
        }
        else
        {
            foregroundMain.startSpeedMultiplier = 0;
            foregroundMain.startSizeMultiplier = 0;
            foregroundEmission.rateOverTimeMultiplier = 0;
        }
        
        if (BackFootGrounded)
        {
            backgroundMain.startSpeedMultiplier = t * MaxBackgroundSpeed;
            backgroundMain.startSizeMultiplier = t * MaxBackgroundScale;
            backgroundEmission.rateOverTimeMultiplier = t * MaxBackgroundEmission;
        }
        else
        {
            backgroundMain.startSpeedMultiplier = 0;
            backgroundMain.startSizeMultiplier = 0;
            backgroundEmission.rateOverTimeMultiplier = 0;
        }
    }
}
