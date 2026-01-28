using UnityEngine;

public class RopeWaterSurface : MonoBehaviour
{
    [Header("Bounds (local space)")]
    [SerializeField] private BoxCollider waterBounds;
    [SerializeField] private Vector3 boundsCenter = Vector3.zero;
    [SerializeField] private Vector3 boundsSize = new Vector3(10f, 2f, 2f);
    [SerializeField] private bool nearIsPositiveZ = true;

    [Header("Rope")]
    [SerializeField, Min(2)] private int segments = 24;
    [SerializeField] private float mass = 1f;
    [SerializeField] private float springToRest = 40f;
    [SerializeField] private float springToNeighbors = 25f;
    [SerializeField] private float damping = 4f;

    [Header("Interaction")]
    [SerializeField] private float impulseScale = 1.5f;
    [SerializeField] private float impulseMax = 8f;
    [SerializeField, Range(0f, 1f)] private float neighborImpulse = 0.4f;
    [SerializeField] private LayerMask affectLayers = ~0;

    [Header("Rendering")]
    [SerializeField] private Material surfaceMaterial;
    [SerializeField] private Material sideMaterial;
    [SerializeField] private string surfaceChildName = "WaterSurfaceMesh";
    [SerializeField] private string sideChildName = "WaterSideMesh";

    private MeshFilter surfaceFilter;
    private MeshFilter sideFilter;
    private MeshRenderer surfaceRenderer;
    private MeshRenderer sideRenderer;
    private Mesh surfaceMesh;
    private Mesh sideMesh;

    private float[] ropeNearY;
    private float[] ropeFarY;
    private float[] ropeNearV;
    private float[] ropeFarV;
    private float[] xPositions;

    private Vector3 lastCenter;
    private Vector3 lastSize;
    private int lastSegments;

    private void Awake()
    {
        EnsureChildren();
        RebuildIfNeeded(true);
        ApplyMaterials();
    }

    private void OnValidate()
    {
        segments = Mathf.Max(2, segments);
        mass = Mathf.Max(0.001f, mass);
        EnsureChildren();
        RebuildIfNeeded(true);
        ApplyMaterials();
    }

    private void FixedUpdate()
    {
        if (segments < 2)
        {
            return;
        }

        RebuildIfNeeded(false);
        SimulateRope(ropeNearY, ropeNearV);
        SimulateRope(ropeFarY, ropeFarV);
    }

    private void LateUpdate()
    {
        if (surfaceMesh == null || sideMesh == null)
        {
            return;
        }

        UpdateMeshes();
    }

    private void EnsureChildren()
    {
        if (surfaceFilter == null)
        {
            var child = transform.Find(surfaceChildName);
            if (child == null)
            {
                child = new GameObject(surfaceChildName).transform;
                child.SetParent(transform, false);
            }

            surfaceFilter = child.GetComponent<MeshFilter>();
            if (surfaceFilter == null) surfaceFilter = child.gameObject.AddComponent<MeshFilter>();
            surfaceRenderer = child.GetComponent<MeshRenderer>();
            if (surfaceRenderer == null) surfaceRenderer = child.gameObject.AddComponent<MeshRenderer>();
        }

        if (sideFilter == null)
        {
            var child = transform.Find(sideChildName);
            if (child == null)
            {
                child = new GameObject(sideChildName).transform;
                child.SetParent(transform, false);
            }

            sideFilter = child.GetComponent<MeshFilter>();
            if (sideFilter == null) sideFilter = child.gameObject.AddComponent<MeshFilter>();
            sideRenderer = child.GetComponent<MeshRenderer>();
            if (sideRenderer == null) sideRenderer = child.gameObject.AddComponent<MeshRenderer>();
        }
    }

    private void ApplyMaterials()
    {
        if (surfaceRenderer != null && surfaceMaterial != null)
        {
            surfaceRenderer.sharedMaterial = surfaceMaterial;
        }
        if (sideRenderer != null && sideMaterial != null)
        {
            sideRenderer.sharedMaterial = sideMaterial;
        }
    }

    private void RebuildIfNeeded(bool force)
    {
        GetBounds(out var center, out var size);
        if (!force && center == lastCenter && size == lastSize && segments == lastSegments)
        {
            return;
        }

        lastCenter = center;
        lastSize = size;
        lastSegments = segments;

        xPositions = new float[segments];
        ropeNearY = new float[segments];
        ropeFarY = new float[segments];
        ropeNearV = new float[segments];
        ropeFarV = new float[segments];

        float xMin = center.x - size.x * 0.5f;
        float xMax = center.x + size.x * 0.5f;
        float yRest = center.y + size.y * 0.5f;

        for (int i = 0; i < segments; i++)
        {
            float t = segments == 1 ? 0f : (float)i / (segments - 1);
            xPositions[i] = Mathf.Lerp(xMin, xMax, t);
            ropeNearY[i] = yRest;
            ropeFarY[i] = yRest;
            ropeNearV[i] = 0f;
            ropeFarV[i] = 0f;
        }

        CreateOrResizeMeshes();
    }

    private void CreateOrResizeMeshes()
    {
        if (surfaceMesh == null)
        {
            surfaceMesh = new Mesh();
            surfaceMesh.name = "WaterSurfaceMeshRuntime";
            surfaceMesh.MarkDynamic();
            surfaceFilter.sharedMesh = surfaceMesh;
        }
        if (sideMesh == null)
        {
            sideMesh = new Mesh();
            sideMesh.name = "WaterSideMeshRuntime";
            sideMesh.MarkDynamic();
            sideFilter.sharedMesh = sideMesh;
        }

        int vertCount = segments * 2;
        var surfaceVerts = new Vector3[vertCount];
        var surfaceUvs = new Vector2[vertCount];
        var surfaceTris = new int[(segments - 1) * 6];

        var sideVerts = new Vector3[vertCount];
        var sideUvs = new Vector2[vertCount];
        var sideTris = new int[(segments - 1) * 6];

        for (int i = 0; i < segments; i++)
        {
            float u = segments == 1 ? 0f : (float)i / (segments - 1);
            surfaceUvs[i] = new Vector2(u, 0f);
            surfaceUvs[i + segments] = new Vector2(u, 1f);

            sideUvs[i] = new Vector2(u, 1f);
            sideUvs[i + segments] = new Vector2(u, 0f);
        }

        int tri = 0;
        for (int i = 0; i < segments - 1; i++)
        {
            int a = i;
            int b = i + 1;
            int c = i + segments;
            int d = i + segments + 1;

            surfaceTris[tri++] = a;
            surfaceTris[tri++] = c;
            surfaceTris[tri++] = b;
            surfaceTris[tri++] = b;
            surfaceTris[tri++] = c;
            surfaceTris[tri++] = d;

            sideTris[tri - 6] = a;
            sideTris[tri - 5] = b;
            sideTris[tri - 4] = c;
            sideTris[tri - 3] = b;
            sideTris[tri - 2] = d;
            sideTris[tri - 1] = c;
        }

        surfaceMesh.Clear();
        surfaceMesh.vertices = surfaceVerts;
        surfaceMesh.uv = surfaceUvs;
        surfaceMesh.triangles = surfaceTris;
        surfaceMesh.RecalculateBounds();
        surfaceMesh.RecalculateNormals();

        sideMesh.Clear();
        sideMesh.vertices = sideVerts;
        sideMesh.uv = sideUvs;
        sideMesh.triangles = sideTris;
        sideMesh.RecalculateBounds();
        sideMesh.RecalculateNormals();
    }

    private void SimulateRope(float[] y, float[] v)
    {
        float dt = Time.fixedDeltaTime;
        float yRest = lastCenter.y + lastSize.y * 0.5f;

        for (int i = 0; i < segments; i++)
        {
            float force = -springToRest * (y[i] - yRest);
            if (i > 0)
            {
                force += -springToNeighbors * (y[i] - y[i - 1]);
            }
            if (i < segments - 1)
            {
                force += -springToNeighbors * (y[i] - y[i + 1]);
            }
            force += -damping * v[i];

            float a = force / mass;
            v[i] += a * dt;
            y[i] += v[i] * dt;
        }
    }

    private void UpdateMeshes()
    {
        GetBounds(out var center, out var size);
        float zNear = center.z + (nearIsPositiveZ ? size.z * 0.5f : -size.z * 0.5f);
        float zFar = center.z - (nearIsPositiveZ ? size.z * 0.5f : -size.z * 0.5f);
        float yBottom = center.y - size.y * 0.5f;

        var surfaceVerts = surfaceMesh.vertices;
        var sideVerts = sideMesh.vertices;

        for (int i = 0; i < segments; i++)
        {
            float x = xPositions[i];
            surfaceVerts[i] = new Vector3(x, ropeNearY[i], zNear);
            surfaceVerts[i + segments] = new Vector3(x, ropeFarY[i], zFar);

            sideVerts[i] = new Vector3(x, ropeNearY[i], zNear);
            sideVerts[i + segments] = new Vector3(x, yBottom, zNear);
        }

        surfaceMesh.vertices = surfaceVerts;
        surfaceMesh.RecalculateBounds();
        surfaceMesh.RecalculateNormals();

        sideMesh.vertices = sideVerts;
        sideMesh.RecalculateBounds();
        sideMesh.RecalculateNormals();
    }

    private void GetBounds(out Vector3 center, out Vector3 size)
    {
        if (waterBounds == null)
        {
            waterBounds = GetComponent<BoxCollider>();
        }

        if (waterBounds != null)
        {
            center = waterBounds.center;
            size = waterBounds.size;
        }
        else
        {
            center = boundsCenter;
            size = boundsSize;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TrySplash(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySplash(other);
    }

    private void TrySplash(Collider other)
    {
        if (((1 << other.gameObject.layer) & affectLayers) == 0)
        {
            return;
        }

        Vector3 velocity = Vector3.zero;
        var rb = other.attachedRigidbody;
        if (rb != null)
        {
            velocity = rb.linearVelocity;
        }

        Vector3 point = other.bounds.center;
        ApplySplash(point, velocity);
    }

    public void ApplySplash(Vector3 worldPoint, Vector3 worldVelocity)
    {
        if (segments < 2)
        {
            return;
        }

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        Vector3 localVelocity = transform.InverseTransformDirection(worldVelocity);

        GetBounds(out var center, out var size);
        float xMin = center.x - size.x * 0.5f;
        float xMax = center.x + size.x * 0.5f;
        float t = (localPoint.x - xMin) / Mathf.Max(0.0001f, xMax - xMin);
        int index = Mathf.Clamp(Mathf.RoundToInt(t * (segments - 1)), 0, segments - 1);

        float impulse = Mathf.Clamp(localVelocity.y * impulseScale, -impulseMax, impulseMax);
        ropeNearV[index] += impulse / mass;
        ropeFarV[index] += impulse / mass;

        if (neighborImpulse > 0f)
        {
            float neighborKick = impulse * neighborImpulse / mass;
            if (index > 0)
            {
                ropeNearV[index - 1] += neighborKick;
                ropeFarV[index - 1] += neighborKick;
            }
            if (index < segments - 1)
            {
                ropeNearV[index + 1] += neighborKick;
                ropeFarV[index + 1] += neighborKick;
            }
        }
    }
}
