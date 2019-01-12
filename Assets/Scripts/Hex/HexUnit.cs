using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class HexUnit : MonoBehaviour
{
    private const float traveSpeed = 4f;
    private const float rotationSpeed = 180f;
    private const int visionRange = 3;

    public static HexUnit unitPrefab;

    public int Speed => 24;

    private HexCell location,currentTracelLocation;
    private float orientation;
    private List<HexCell> pathToTravel;
    private Coroutine cor;

    public HexCell Location
    {
        get => location;

        set
        {
            if (location)
            {
                HexGrid.Instance.DecreaseVisibility(location, visionRange);
                location.Unit = null;
            }

            location = value;
            value.Unit = this;
            HexGrid.Instance.IncreaseVisibility(value, visionRange);
            transform.localPosition = value.Position;
        }
    }

    public float Orientation
    {
        get => orientation;

        set
        {
            orientation = value;
            transform.localRotation = Quaternion.Euler(0, value, 0);
        }
    }

    public int VisionRange => visionRange;

    /// <summary>
    /// 游戏中忽然重新编译最用
    /// </summary>
    private void OnEnable()
    {
        if (location)
        {
            transform.localPosition = location.Position;
            if (currentTracelLocation)
            {
                HexGrid.Instance.IncreaseVisibility(location, visionRange);
                HexGrid.Instance.DecreaseVisibility(currentTracelLocation, visionRange);
                currentTracelLocation = null;
            }
        }
    }

    /// <summary>
    /// 重新设置位置
    /// </summary>
    public void ValidateLocation()
    {
        transform.localPosition = location.Position;
    }

    /// <summary>
    /// 死亡
    /// </summary>
    public void Die()
    {
        if (location)
        {
            HexGrid.Instance.DecreaseVisibility(location, visionRange);
        }

        location.Unit = null;
        Destroy(gameObject);
    }

    /// <summary>
    /// 写到Unit里面是因为 这个有效性 可能根据单位的类型而改变
    /// </summary>
    public bool IsValidDestination(HexCell cell)
    {
        return cell.IsExplored && !cell.IsUnderwater && !cell.Unit;
    }

    /// <summary>
    /// 写到Unit里面是因为 MoveCost 可能根据单位的类型而改变
    /// </summary>
    public int GetMoveCost(HexCell fromCell, HexCell toCell, HexDirection direction)
    {
        HexEdgeType edgeType = fromCell.GetEdgeType(toCell);
        if (edgeType == HexEdgeType.Cliff)
        {
            return -1;
        }
        if (fromCell.Walled != toCell.Walled)
        {
            return -1;
        }

        int moveCost;
        if (fromCell.HasRoadThroughEdge(direction))
        {
            moveCost = 1;
        }
        else
        {
            moveCost = edgeType == HexEdgeType.Flat ? 5 : 10;
            moveCost += toCell.UrbanLevel + toCell.FarmLevel + toCell.PlantLevel;
        }

        return moveCost;
    }

    /// <summary>
    /// 走路
    /// </summary>
    public void Travel(List<HexCell> path)
    {
        location.Unit = null;
        location = path[path.Count - 1];
        location.Unit = this;
        pathToTravel = path;
        if (cor != null)
        {
            StopCoroutine(cor);
        }

        cor = StartCoroutine(TravelPath());
    }

    /// <summary>
    /// 转向目标
    /// </summary>
    private IEnumerator LookAt(Vector3 point)
    {
        point.y = transform.localPosition.y;
        Quaternion fromRotation = transform.localRotation;
        Quaternion toRotation =
            Quaternion.LookRotation(point - transform.localPosition);
        float angle = Quaternion.Angle(fromRotation, toRotation);
        float speed = rotationSpeed / angle;

        if (angle > 0f)
        {
            for (float t = Time.deltaTime * speed; t < 1f; t += Time.deltaTime * speed)
            {
                transform.localRotation = Quaternion.Slerp(fromRotation, toRotation, t);
                yield return null;
            }
        }

        transform.LookAt(point);
        orientation = transform.localRotation.eulerAngles.y;
    }

    /// <summary>
    /// 根据路径行走
    /// </summary>
    public IEnumerator TravelPath()
    {
        Vector3 a, b, c = pathToTravel[0].Position;
        yield return LookAt(pathToTravel[1].Position);
        HexGrid.Instance.DecreaseVisibility(
            currentTracelLocation?? pathToTravel[0], visionRange);

        //第一帧是移动的
        float t = Time.deltaTime * traveSpeed;
        for (int i = 1; i <= pathToTravel.Count; i++)
        {
            var tempI = i - 1;

            a = c;
            b = pathToTravel[tempI].Position;
            if (i == pathToTravel.Count)
            {
                c = b;
            }
            else
            {
                currentTracelLocation = pathToTravel[i];
                c = (b + currentTracelLocation.Position) * 0.5f;
            }

            HexGrid.Instance.IncreaseVisibility(pathToTravel[tempI], visionRange);
            //用这种-1 的模式 因为帧数太卡
            //Time.deltaTime过大 可能一步走的过大 
            //第二次重置了 过大+继续走=不正确
            //如果是-1模式 则对第一步做补偿
            for (; t < 1f; t += Time.deltaTime * traveSpeed)
            {
  
                transform.localPosition = Bezier.GetPoint(a, b, c, t);
                Vector3 d = Bezier.GetDerivative(a, b, c, t);
                d.y = 0;
                transform.localRotation = Quaternion.LookRotation(d);
                yield return null;
            }

            if (i != pathToTravel.Count)
            {
                HexGrid.Instance.DecreaseVisibility(pathToTravel[tempI], visionRange);
            }

            t -= 1f;
        }

        currentTracelLocation = null;

        transform.localPosition = location.Position;
        ListPool<HexCell>.Add(pathToTravel);
        cor = null;
        pathToTravel = null;
    }

    /// <summary>
    /// 保存
    /// </summary>
    public void Save(MyWriter writer)
    {
        location.coordinates.Save(writer);
        writer.Write(orientation);
    }

    /// <summary>
    /// 读取
    /// </summary>
    public static void Load(MyReader reader, HexGrid grid)
    {
        var coordinates = HexCoordinates.Load(reader);
        var orientation = reader.ReadSingle();
        grid.AddUnit(Instantiate(unitPrefab), grid.GetCell(coordinates)
            , orientation);
    }

    /*
    private void OnDrawGizmos()
    {
        if (pathToTravel == null || pathToTravel.Count == 0)
        {
            return;
        }

        Vector3 a, b, c = pathToTravel[0].Position;

        for (int i = 1; i <= pathToTravel.Count; i++)
        {
            a = c;
            b = pathToTravel[i - 1].Position;
            if (i == pathToTravel.Count)
            {
                c = b;
            }
            else
            {
                c = (pathToTravel[i - 1].Position + pathToTravel[i].Position) * 0.5f;
            }

            for (float t = 0f; t < 1f; t += 0.1f)
            {
                Gizmos.DrawSphere(Bezier.GetPoint(a, b, c, t), 2f);
            }
        }
    }
    */
}