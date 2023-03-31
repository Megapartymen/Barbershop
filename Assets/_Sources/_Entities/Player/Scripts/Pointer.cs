using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Pointer : MonoBehaviour
{
    public PointerTarget PointerTarget;
    public Vector3 Point;
    public bool IsPointerEnabled;
    
    [SerializeField] private float _initialVelocity;
    [SerializeField] private LineRenderer _line;
    [SerializeField] private Transform _hand;
    [SerializeField] private float _step;
    [SerializeField] private Transform _startPoint;
    [SerializeField] private LayerMask _layerMask;
    [SerializeField] private GameObject _target;
    
    private bool _isCanCalculatePath;
    

    private void Update()
    {
        if (IsPointerEnabled)
            RealizeCurve();
        else
        {
            _target.transform.position = transform.position;
            Point = transform.position;
            _target.SetActive(false);
            EraseCurve();
        }
    }

    private void RealizeCurve()
    {
        float angle = GetAngle() * Mathf.Deg2Rad;

        if (GetPathTarget(_hand.forward, _initialVelocity, angle, _step) != Vector3.zero)
        {
            if (!_line.enabled) _line.enabled = true;
            
            Vector3 direction = GetPathTarget(_hand.forward, _initialVelocity, angle, _step) - _startPoint.position;
            Vector3 groundDirection = new Vector3(direction.x, 0, direction.z);
            Vector3 targetPosition = new Vector3(groundDirection.magnitude, direction.y, 0);
            float velocityInitial;
            float time;
        
            CalculateCurve(targetPosition, angle, out velocityInitial, out time);
            DrawCurve(groundDirection.normalized, velocityInitial, angle, time, _step);
            
            _target.SetActive(true);
        }
        else
        {
            _target.SetActive(false);
            EraseCurve();
        }
    }

    private void CalculateCurve(Vector3 targetPosition, float angle, out float velocityInitial, out float time)
    {
        float xt = targetPosition.x;
        float yt = targetPosition.y;
        float g = -Physics.gravity.y;

        float v1 = Mathf.Pow(xt, 2) * g;
        float v2 = 2 * xt * Mathf.Sin(angle) * Mathf.Cos(angle);
        float v3 = 2 * yt * Mathf.Pow(Mathf.Cos(angle), 2);
        velocityInitial = Mathf.Sqrt(v1 / (v2 - v3));

        time = xt / (velocityInitial * Mathf.Cos(angle));
    }

    private float GetAngle()
    {
        return _hand.localRotation.eulerAngles.x * -1;
    }

    private Vector3 GetPathTarget(Vector3 direction, float velocityInitial, float angle, float step)
    {
        step = Mathf.Max(0.01f, step);
        float totalTime = 5;
        Vector3 previousPosition = _hand.position;
        Vector3 newPosition = _hand.position;

        for (float i = 0; i < totalTime; i += step)
        {
            float x = velocityInitial * i * Mathf.Cos(angle);
            float y = velocityInitial * i * Mathf.Sin(angle) - 0.5f * -Physics.gravity.y * Mathf.Pow(i, 2);
            newPosition = _startPoint.position + direction*x + Vector3.up * y;

            if(Physics.Linecast(previousPosition, newPosition, out RaycastHit hitInfo, _layerMask))
            {
                Point = Vector3.Lerp(Point, hitInfo.point, 0.05f);
                // Point = hitInfo.point;
                
                _target.transform.position = Point;
                return Point;
            }

            previousPosition = newPosition;
        }

        return Vector3.zero;
    }
    
    private void DrawCurve(Vector3 direction, float velocityInitial, float angle, float time, float step)
    {
        step = Mathf.Max(0.01f, step);
        _line.positionCount = (int) (time / step) + 2;
        int count = 0;
        Vector3 previousPosition = _hand.position;
        Vector3 newPosition = _hand.position;

        for (float i = 0; i < time; i += step)
        {
            float x = velocityInitial * i * Mathf.Cos(angle);
            float y = velocityInitial * i * Mathf.Sin(angle) - 0.5f * -Physics.gravity.y * Mathf.Pow(i, 2);
            newPosition = _startPoint.position + direction*x + Vector3.up * y;
            
            _line.SetPosition(count, newPosition);

            previousPosition = newPosition;
            count++;
        }
        
        float xFinal = velocityInitial * time * Mathf.Cos(angle);
        float yFinal = velocityInitial * time * Mathf.Sin(angle) - 0.5f * -Physics.gravity.y * Mathf.Pow(time, 2);
        _line.SetPosition(count, _startPoint.position + direction*xFinal + Vector3.up * yFinal);
    }

    private void EraseCurve()
    {
        if (_line.enabled) _line.enabled = false;
    }
}
