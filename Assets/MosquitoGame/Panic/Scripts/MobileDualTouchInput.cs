using UnityEngine;

public class MobileDualTouchInput
{
    private const float TapMaxDuration = 0.28f;
    private const float TapMaxMovement = 18f;

    private int leftFingerId = -1;
    private int rightFingerId = -1;
    private Vector2 leftLastPosition;
    private Vector2 rightLastPosition;
    private float rightFingerDownTime;
    private Vector2 rightFingerDownPosition;

    public Vector2 MoveDelta { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public bool FirePressed { get; private set; }
    public bool DashPressed { get; private set; }

    public void ResetFrame()
    {
        MoveDelta = Vector2.zero;
        LookDelta = Vector2.zero;
        FirePressed = false;
        DashPressed = false;
    }

    public void Update(bool enableTouch, bool enableKeyboardFallback)
    {
        ResetFrame();

        if (enableTouch && Input.touchCount > 0)
            UpdateTouch();
        else if (enableKeyboardFallback)
            UpdateKeyboard();
    }

    private void UpdateTouch()
    {
        for (var i = 0; i < Input.touchCount; i++)
        {
            var touch = Input.GetTouch(i);
            var isLeftHalf = touch.position.x < Screen.width * 0.5f;

            if (touch.phase == TouchPhase.Began)
            {
                if (isLeftHalf && leftFingerId < 0)
                {
                    leftFingerId = touch.fingerId;
                    leftLastPosition = touch.position;
                }
                else if (!isLeftHalf && rightFingerId < 0)
                {
                    rightFingerId = touch.fingerId;
                    rightLastPosition = touch.position;
                    rightFingerDownTime = Time.time;
                    rightFingerDownPosition = touch.position;
                }
            }

            if (touch.fingerId == leftFingerId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    var delta = touch.position - leftLastPosition;
                    MoveDelta += delta * 0.02f;
                    leftLastPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    leftFingerId = -1;
                }
            }

            if (touch.fingerId == rightFingerId)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    var delta = touch.position - rightLastPosition;
                    LookDelta += delta * PanicGameConstants.MosquitoLookSensitivity;
                    rightLastPosition = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    var duration = Time.time - rightFingerDownTime;
                    var movement = Vector2.Distance(touch.position, rightFingerDownPosition);
                    if (duration <= TapMaxDuration && movement <= TapMaxMovement)
                        FirePressed = true;

                    rightFingerId = -1;
                }
            }
        }
    }

    private void UpdateKeyboard()
    {
        MoveDelta = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        LookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y")) * 3f;
        FirePressed = Input.GetMouseButtonDown(0);
        DashPressed = Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.LeftShift);
    }
}
