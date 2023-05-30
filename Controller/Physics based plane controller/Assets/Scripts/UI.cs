using UnityEngine;


public class UI : MonoBehaviour
{ 
	[SerializeField] private RectTransform pitchIndicator; 
	[SerializeField] private RectTransform rollIndicator;

	public void UpdateIndicators(float pitch, float roll)
	{ 
		if (pitch > 25f)
			pitchIndicator.rotation = Quaternion.Euler(0, 0, pitch); 
		
		rollIndicator.rotation = Quaternion.Euler(0, 0, roll);
	}
}