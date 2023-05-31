using TMPro;

using UnityEngine;


public class UI : MonoBehaviour
{ 
	[SerializeField] private RectTransform pitchIndicator; 
	[SerializeField] private RectTransform rollIndicator;
	[SerializeField] private RectTransform compassIndicator;

	[SerializeField] private TextMeshProUGUI altitude;
	
	[SerializeField] private RectTransform throttleHandle;
	[SerializeField] private float throttlePosMin;
	[SerializeField] private float throttlePosMax;
	
	public void UpdateIndicators(float pitch, float roll, float yaw, float throttle, float _altitude)
	{
		pitchIndicator.rotation = Quaternion.Euler(0, 0, pitch);
		rollIndicator.rotation = Quaternion.Euler(0, 0, -roll);
		compassIndicator.rotation = Quaternion.Euler(0, 0, yaw);

		float throt = Mathf.Lerp(throttlePosMin, throttlePosMax, throttle);
		
		throttleHandle.localPosition = new Vector3(-23f, 
			throt, 1);

		altitude.text = "Altitude: " + ((int)_altitude).ToString();
	}
}