using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectManipulator : MonoBehaviour
{
    public GameObject ARObject;

    [SerializeField] private Camera arCamera;

    private bool isArObjectSelected;
    private string tagArObjects = "ARObject";

    private Vector2 initialTouchPos;

    [SerializeField] private float speedMove = 4.0f;
    [SerializeField] private float speedRotate = 5.0f;
    [SerializeField] private float scaleFactor = 0.1f;

    private float screenFactor = 0.0001f;

    private float touchDistance;
    private Vector2 touchPositionDiff;
    private float rotationTolerance = 1.5f;
    private float scaleTolerance = 25f;

    void Update()
    {
        if (Input.touchCount > 0)
        {
            Touch touchOne = Input.GetTouch(0);

            if (Input.touchCount == 1)
            {
                if (touchOne.phase == TouchPhase.Began)
                {
                    initialTouchPos = touchOne.position;
                    isArObjectSelected = CheckTouch(initialTouchPos);
                }

                if (touchOne.phase == TouchPhase.Moved && isArObjectSelected)
                {
                    Vector2 diffPos = (touchOne.position - initialTouchPos) * screenFactor;
                    ARObject.transform.position = ARObject.transform.position +
                        new Vector3(diffPos.x * speedMove, diffPos.y * speedMove, 0);

                    initialTouchPos = touchOne.position;
                }
            }

            if (Input.touchCount == 2)
            {
                Touch touchTwo = Input.GetTouch(1);

                if (touchOne.phase == TouchPhase.Began || touchTwo.phase == TouchPhase.Began)
                {
                    touchPositionDiff = touchTwo.position - touchOne.position;
                    touchDistance = Vector2.Distance(touchTwo.position, touchOne.position);
                }

                if (touchOne.phase == TouchPhase.Moved || touchTwo.phase == TouchPhase.Moved)
                {
                    Vector2 currentTouchPosDiff = touchTwo.position - touchOne.position;
                    float currentTouchDis = Vector2.Distance(touchTwo.position, touchOne.position);

                    float diffDis = currentTouchDis - touchDistance;

                    if (Mathf.Abs(diffDis) > scaleTolerance)
                    {
                        Vector3 newScale = ARObject.transform.localScale + Mathf.Sign(diffDis) * Vector3.one * scaleFactor;
                        ARObject.transform.localScale = Vector3.Lerp(ARObject.transform.localScale, newScale, 0.05f);
                    }

                    float angle = Vector2.SignedAngle(touchPositionDiff, currentTouchPosDiff);

                    if (Mathf.Abs(angle) > rotationTolerance)
                    {
                        ARObject.transform.rotation =
                            Quaternion.Euler(0, ARObject.transform.rotation.eulerAngles.y - Mathf.Sign(angle) * speedRotate, 0);
                    }

                    touchDistance = currentTouchDis;
                    touchPositionDiff = currentTouchPosDiff;

                }
            }
        }
    }

    private bool CheckTouch(Vector2 touchPos)
    {
        Ray ray = arCamera.ScreenPointToRay(touchPos);

        if (Physics.Raycast(ray, out RaycastHit hitArObject))
        {
            if (hitArObject.collider.CompareTag(tagArObjects))
            {
                ARObject = hitArObject.transform.gameObject;
                return true;
            }
        }

        return false;
    }

}