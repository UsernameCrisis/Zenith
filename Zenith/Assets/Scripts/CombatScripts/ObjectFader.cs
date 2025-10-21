using UnityEngine;

public class ObjectFader : MonoBehaviour
{
    [SerializeField] private float fadeSpeed, fadeAmount;
    [SerializeField] private float originalOpacity;
    
    private Material mat;

    public bool DoFade = false;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        originalOpacity = mat.color.a;
    }

    void Update()
    {
        if (DoFade)
            FadeNow();
        else
            ResetFade();
    }

    private void FadeNow()
    {
        Color currColor = mat.color;
        Color smoothColor = new(currColor.r, currColor.g, currColor.b, Mathf.Lerp(currColor.a, fadeAmount, fadeSpeed * Time.deltaTime));
        mat.color = smoothColor;
    }

    private void ResetFade()
    {
        Color currColor = mat.color;
        Color smoothColor = new(currColor.r, currColor.g, currColor.b, Mathf.Lerp(currColor.a, originalOpacity, fadeSpeed * Time.deltaTime));
        mat.color = smoothColor;
    }
}
