using UnityEngine;
using System.Collections.Generic;

public class RandomTransform : MonoBehaviour
{

    public enum SamplingType { Uniform, Gaussian }
    public SamplingType samplingType = SamplingType.Uniform;

    [System.Serializable]
    public class TransformAxis
    {
        public bool isRandom = false;
        public Vector2 range = new Vector2(-10f, 10f);
    }

    public TransformAxis positionX = new TransformAxis();
    public TransformAxis positionY = new TransformAxis();
    public TransformAxis positionZ = new TransformAxis();

    public TransformAxis rotationX = new TransformAxis();
    public TransformAxis rotationY = new TransformAxis();
    public TransformAxis rotationZ = new TransformAxis();

    public TransformAxis scaleX = new TransformAxis();
    public TransformAxis scaleY = new TransformAxis();
    public TransformAxis scaleZ = new TransformAxis();

    public void SampleTransform(int index)
    {
        Random.InitState(index);
        
        Vector3 newPosition = transform.position;
        Vector3 newScale = transform.localScale;
        Vector3 newRotation = transform.localRotation.eulerAngles;

        if (positionX.isRandom) newPosition.x = SampleValue(positionX.range);
        if (positionY.isRandom) newPosition.y = SampleValue(positionY.range);
        if (positionZ.isRandom) newPosition.z = SampleValue(positionZ.range);

        if (scaleX.isRandom) newScale.x = SampleValue(scaleX.range);
        if (scaleY.isRandom) newScale.y = SampleValue(scaleY.range);
        if (scaleZ.isRandom) newScale.z = SampleValue(scaleZ.range);

        if (rotationX.isRandom) newRotation.x = SampleValue(rotationX.range);
        if (rotationY.isRandom) newRotation.y = SampleValue(rotationY.range);
        if (rotationZ.isRandom) newRotation.z = SampleValue(rotationZ.range);

        transform.position = newPosition;
        transform.localScale = newScale;
        transform.localRotation = Quaternion.Euler(newRotation);
    }

    float SampleValue(Vector2 range)
    {
        float newValue = 0f;

        switch (samplingType)
        {
            case SamplingType.Uniform:
                newValue = Random.Range(range.x, range.y);
                break;
            case SamplingType.Gaussian:
                newValue = Mathf.Sqrt(-2.0f * Mathf.Log(Random.value)) * Mathf.Sin(2.0f * Mathf.PI * Random.value);
                newValue = newValue / 2.0f + 0.5f;  // Scale to 0 -> 1
                newValue = newValue * (range.y - range.x) + range.x;  // Scale within range
                break;
        }

        return newValue;
    }

    public Dictionary<string, object> GetTransformData()
    {
        Dictionary<string, object> data = new Dictionary<string, object>();

        data["Position"] = transform.position;
        data["Rotation"] = transform.localRotation.eulerAngles;
        data["Scale"] = transform.localScale;

        return data;
    }
}
