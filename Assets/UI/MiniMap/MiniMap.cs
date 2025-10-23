using UnityEngine;

public class MiniMap : MonoBehaviour
{
    private Transform _playerTransform;
    private Transform _playerIcon;
    private float _playerIconOffset = 0f;

    private void Start()
    {
        GameObject player = GameObject.Find("Player");
        GameObject playerIcon = GameObject.Find("PlayerIcon");

        if (player != null)
            _playerTransform = player.GetComponent<Transform>();

        if (playerIcon != null)
            _playerIcon = playerIcon.GetComponent<Transform>();
    }

    void Update()
    {
        if (_playerTransform != null && _playerIcon != null)
        {
            // Match the sprite's position to the player's position
            _playerIcon.transform.position = new Vector3(_playerTransform.position.x, transform.position.y - _playerIconOffset, _playerTransform.position.z);

            // Calculate the desired rotation for the player icon
            Quaternion desiredRotation = Quaternion.Euler(90f, _playerTransform.eulerAngles.y, 0f);

            // Match the player icon's rotation to the desired rotation
            _playerIcon.rotation = desiredRotation;

        }
    }
}
