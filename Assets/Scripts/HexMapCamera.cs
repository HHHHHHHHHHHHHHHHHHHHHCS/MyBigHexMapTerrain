using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 摄像机用
/// </summary>
public class HexMapCamera : MonoBehaviour
{
    private static HexMapCamera instance;

    public HexGrid grid;
    public float stickMinZoom = -250, stickMaxZoom = -45; //视野缩放摄像机的位置
    public float swivelMinZoom = 90, swivelMaxZoom = 45; //视野缩放 摄像机的观察角度
    public float moveSpeedMinZoom = 400, moveSpeedMaxZoom = 100; //根据视野缩放摄像机的移动速度
    public float rotationSpeed = 180; //摄像机的旋转速度

    private Transform swivel, stick;
    private float zoom = 1f;
    private float rotationAngle;

    public bool Locked
    {
        set => enabled = !value;
    }

    public static HexMapCamera Instance
    {
        get => instance;
    }


    private void Awake()
    {
        instance = this;
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }

    private void Update()
    {
        float zoomDelta = Input.GetAxis("Mouse ScrollWheel");
        if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
        }

        float rotationDelta = Input.GetAxis("Rotation");
        if (rotationDelta != 0f)
        {
            AdjustRotation(rotationDelta);
        }

        float xDelta = Input.GetAxis("Horizontal");
        float zDelta = Input.GetAxis("Vertical");
        if (xDelta != 0f || zDelta != 0f)
        {
            AdjustPosition(xDelta, zDelta);
        }
    }

    /// <summary>
    /// 缩放
    /// </summary>
    /// <param name="delta"></param>
    private void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);

        float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
        stick.localPosition = new Vector3(0, 0, distance);

        float angle = Mathf.Lerp(swivelMinZoom, swivelMaxZoom, zoom);
        swivel.localRotation = Quaternion.Euler(angle, 0, 0);
    }

    /// <summary>
    /// 移动
    /// </summary>
    /// <param name="xDelta"></param>
    /// <param name="zDelta"></param>
    private void AdjustPosition(float xDelta, float zDelta)
    {
        Vector3 direction = transform.localRotation * new Vector3(xDelta, 0f, zDelta).normalized;
        float damping = Mathf.Max(Mathf.Abs(xDelta), Mathf.Abs(zDelta));
        float distance = Mathf.Lerp(moveSpeedMinZoom, moveSpeedMaxZoom, zoom)
                         * damping * Time.deltaTime;

        Vector3 position = transform.localPosition;
        position += direction * distance;
        transform.localPosition = ClampPosition(position);
    }

    /// <summary>
    /// 限制摄像机的位置
    /// </summary>
    /// <param name="postion"></param>
    /// <returns></returns>
    private Vector3 ClampPosition(Vector3 postion)
    {
        float xMax = (grid.cellCountX * HexMetrics.chunkSizeX - 0.5f)
                     * (2f * HexMetrics.innerRadius);
        postion.x = Mathf.Clamp(postion.x, 0f, xMax);


        float zMax = (grid.cellCountZ * HexMetrics.chunkSizeZ - 1f)
                     * (1.5f * HexMetrics.outerRadius);
        return postion;
    }

    /// <summary>
    /// 旋转
    /// </summary>
    /// <param name="delta"></param>
    private void AdjustRotation(float delta)
    {
        rotationAngle += delta * rotationSpeed * Time.deltaTime;

        if (rotationAngle < 0f)
        {
            rotationAngle += 360f;
        }
        else if (rotationAngle >= 360f)
        {
            rotationAngle -= 360f;
        }

        transform.localRotation = Quaternion.Euler(0f, rotationAngle, 0f);
    }

    public void ValidatePosition()
    {
        AdjustPosition(0f, 0f);
        //zoom = 0.5f;
        //AdjustZoom(0);
    }
}