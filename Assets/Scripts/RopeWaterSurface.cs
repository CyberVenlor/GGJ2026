using UnityEngine;

[ExecuteAlways]
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
    [SerializeField] private float sideHeightOffset = 0f;

    [Header("Interaction")]
    [SerializeField] private float impulseScale = 1.5f;
    [SerializeField] private float impulseMax = 8f;
    [SerializeField, Range(0f, 1f)] private float neighborImpulse = 0.4f;
    [SerializeField, Range(0f, 10f)] private float sideImpulseRandom = 0.15f;
    [SerializeField] private LayerMask affectLayers = ~0;

    [Header("Buoyancy")]
    [SerializeField] private float buoyancyStrength = 25f;
    [SerializeField] private float buoyancyDamping = 4f;
    [SerializeField] private float maxBuoyancyForce = 200f;
    [SerializeField] private float buoyancySampleOffset = 0f;
    [SerializeField] private float waterQuadraticDrag = 2.5f;
    [SerializeField] private float horizontalToUpImpulse = 0.5f;

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

    private void OnEnable()
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
        if (!Application.isPlaying)
        {
            return;
        }

        if (segments < 2)
        {
            return;
        }

        RebuildIfNeeded(false);
        float yRestNear = lastCenter.y + lastSize.y * 0.5f;
        float yRestFar = yRestNear + sideHeightOffset;
        SimulateRope(ropeNearY, ropeNearV, yRestNear);
        SimulateRope(ropeFarY, ropeFarV, yRestFar);
    }

    private void LateUpdate()
    {
        if (!Application.isPlaying)
        {
            EnsureChildren();
            RebuildIfNeeded(false);
        }

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
        float yRestNear = center.y + size.y * 0.5f;
        float yRestFar = yRestNear + sideHeightOffset;

        for (int i = 0; i < segments; i++)
        {
            float t = segments == 1 ? 0f : (float)i / (segments - 1);
            xPositions[i] = Mathf.Lerp(xMin, xMax, t);
            ropeNearY[i] = yRestNear;
            ropeFarY[i] = yRestFar;
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

        int sideVertCount = segments * 4;
        var sideVerts = new Vector3[sideVertCount];
        var sideUvs = new Vector2[sideVertCount];
        var sideTris = new int[(segments - 1) * 12];

        for (int i = 0; i < segments; i++)
        {
            float u = segments == 1 ? 0f : (float)i / (segments - 1);
            surfaceUvs[i] = new Vector2(u, 0f);
            surfaceUvs[i + segments] = new Vector2(u, 1f);

            // Near strip UVs
            sideUvs[i] = new Vector2(u, 1f);
            sideUvs[i + segments] = new Vector2(u, 0f);

            // Far strip UVs
            int farOffset = segments * 2;
            sideUvs[farOffset + i] = new Vector2(u, 1f);
            sideUvs[farOffset + i + segments] = new Vector2(u, 0f);
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

            // Near side strip
            int sideTriBase = (i * 6);
            sideTris[sideTriBase + 0] = a;
            sideTris[sideTriBase + 1] = b;
            sideTris[sideTriBase + 2] = c;
            sideTris[sideTriBase + 3] = b;
            sideTris[sideTriBase + 4] = d;
            sideTris[sideTriBase + 5] = c;

            // Far side strip (offset)
            int farOffset = segments * 2;
            int fa = farOffset + i;
            int fb = farOffset + i + 1;
            int fc = farOffset + i + segments;
            int fd = farOffset + i + segments + 1;
            int farTriBase = (segments - 1) * 6 + (i * 6);
            sideTris[farTriBase + 0] = fa;
            sideTris[farTriBase + 1] = fc;
            sideTris[farTriBase + 2] = fb;
            sideTris[farTriBase + 3] = fb;
            sideTris[farTriBase + 4] = fc;
            sideTris[farTriBase + 5] = fd;
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

    private void SimulateRope(float[] y, float[] v, float yRest)
    {
        float dt = Time.fixedDeltaTime;

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

        int farOffset = segments * 2;
        for (int i = 0; i < segments; i++)
        {
            float x = xPositions[i];
            surfaceVerts[i] = new Vector3(x, ropeNearY[i], zNear);
            surfaceVerts[i + segments] = new Vector3(x, ropeFarY[i], zFar);

            // Near side strip
            sideVerts[i] = new Vector3(x, ropeNearY[i], zNear);
            sideVerts[i + segments] = new Vector3(x, yBottom, zNear);

            // Far side strip
            sideVerts[farOffset + i] = new Vector3(x, ropeFarY[i], zFar);
            sideVerts[farOffset + i + segments] = new Vector3(x, yBottom, zFar);
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
        TryBuoyancy(other);
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

    private void TryBuoyancy(Collider other)
    {
        if (((1 << other.gameObject.layer) & affectLayers) == 0)
        {
            return;
        }

        var rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        Vector3 worldPoint = other.bounds.center;
        worldPoint.y += buoyancySampleOffset;
        ApplyBuoyancy(worldPoint, rb);
    }

    private void ApplyBuoyancy(Vector3 worldPoint, Rigidbody rb)
    {
        float waterTopY = GetWaterTopYWorld();
        float depth = waterTopY - worldPoint.y;
        if (depth <= 0f)
        {
            return;
        }

        float upward = depth * buoyancyStrength;
        float damping = -rb.linearVelocity.y * buoyancyDamping;
        float force = Mathf.Clamp(upward + damping, -maxBuoyancyForce, maxBuoyancyForce);
        rb.AddForce(Vector3.up * force, ForceMode.Force);

        if (waterQuadraticDrag > 0f)
        {
            float vx = rb.linearVelocity.x;
            if (Mathf.Abs(vx) > 0.0001f)
            {
                float dragImpulse = -vx * waterQuadraticDrag;
                rb.AddForce(Vector3.right * dragImpulse, ForceMode.Impulse);
            }
        }

        if (horizontalToUpImpulse > 0f)
        {
            float vx = rb.linearVelocity.x;
            if (Mathf.Abs(vx) > 0.0001f)
            {
                rb.AddForce(Vector3.up * horizontalToUpImpulse, ForceMode.Impulse);
            }
        }
    }

    private float GetWaterTopYWorld()
    {
        if (waterBounds != null)
        {
            return waterBounds.bounds.max.y;
        }

        GetBounds(out var center, out var size);
        Vector3 topLocal = center + Vector3.up * size.y * 0.5f;
        return transform.TransformPoint(topLocal).y;
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

        float impulse = localVelocity.y * impulseScale;
        float nearImpulse = ApplyRandomSideImpulse(impulse);
        float farImpulse = ApplyRandomSideImpulse(impulse);
        nearImpulse = Mathf.Clamp(nearImpulse, -impulseMax, impulseMax);
        farImpulse = Mathf.Clamp(farImpulse, -impulseMax, impulseMax);
        ropeNearV[index] += nearImpulse / mass;
        ropeFarV[index] += farImpulse / mass;

        if (neighborImpulse > 0f)
        {
            float neighborKickNear = nearImpulse * neighborImpulse / mass;
            float neighborKickFar = farImpulse * neighborImpulse / mass;
            if (index > 0)
            {
                ropeNearV[index - 1] += neighborKickNear;
                ropeFarV[index - 1] += neighborKickFar;
            }
            if (index < segments - 1)
            {
                ropeNearV[index + 1] += neighborKickNear;
                ropeFarV[index + 1] += neighborKickFar;
            }
        }
    }

    private float ApplyRandomSideImpulse(float impulse)
    {
        if (sideImpulseRandom <= 0f)
        {
            return impulse;
        }

        float jitter = Random.Range(-sideImpulseRandom, sideImpulseRandom);
        return impulse * (1f + jitter);
    }

    private float GetSurfaceHeight(Vector3 localPoint)
    {
        GetBounds(out var center, out var size);
        float xMin = center.x - size.x * 0.5f;
        float xMax = center.x + size.x * 0.5f;
        float tX = (localPoint.x - xMin) / Mathf.Max(0.0001f, xMax - xMin);
        tX = Mathf.Clamp01(tX);
        float fIndex = tX * (segments - 1);
        int i0 = Mathf.Clamp(Mathf.FloorToInt(fIndex), 0, segments - 1);
        int i1 = Mathf.Clamp(i0 + 1, 0, segments - 1);
        float lerpX = fIndex - i0;

        float yNear = Mathf.Lerp(ropeNearY[i0], ropeNearY[i1], lerpX);
        float yFar = Mathf.Lerp(ropeFarY[i0], ropeFarY[i1], lerpX);

        float zNear = center.z + (nearIsPositiveZ ? size.z * 0.5f : -size.z * 0.5f);
        float zFar = center.z - (nearIsPositiveZ ? size.z * 0.5f : -size.z * 0.5f);
        float tZ = (localPoint.z - zNear) / Mathf.Max(0.0001f, zFar - zNear);
        tZ = Mathf.Clamp01(tZ);

        return Mathf.Lerp(yNear, yFar, tZ);
    }
}
