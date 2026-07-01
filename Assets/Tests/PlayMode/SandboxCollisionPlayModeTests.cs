using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

/// <summary>
/// PlayMode coverage for the prototype collision rules specified in
/// <c>docs/wiki/rendering-and-collision.md</c> (ticket P1-COLL-001): a player built from
/// <see cref="SandboxPlayerController"/> standing on, jumping off, walking into, and not
/// tunneling through run-merged terrain colliders produced by <see cref="SandboxChunkRenderer"/>.
///
/// The scene is built in isolation (no <c>SandboxWorld</c> bootstrap): a single
/// <see cref="SandboxChunk"/> is filled with solid tiles and rebuilt through the real renderer
/// so genuine <c>BoxCollider2D</c> colliders exist, then a player GameObject is dropped onto it
/// and physics is stepped with <c>WaitForFixedUpdate</c>.
/// </summary>
public sealed class SandboxCollisionPlayModeTests
{
    // Floor: local rows [0, FloorRows) fully solid -> solid world region y in [0, FloorRows),
    // top surface at world y == FloorRows.
    private const int FloorRows = 3;
    private const float FloorTopY = FloorRows;

    // A wall column rising from the floor, used by the walk/wall-stop test.
    private const int WallX = 10;
    private const int WallTopRowExclusive = 6; // solid rows 3,4,5 above the floor.

    private const float TileSize = 1f;
    private const float PlayerHalfHeight = 0.9f; // collider size.y (1.8) / 2.
    private const float RestCenterY = FloorTopY + PlayerHalfHeight; // ~3.9 when grounded.

    private GameObject floorObject;
    private GameObject playerObject;
    private SandboxPlayerController player;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        BuildFloorAndWall();
        yield return null; // let renderer/colliders register with the physics scene.
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        if (playerObject != null)
        {
            Object.Destroy(playerObject);
        }

        if (floorObject != null)
        {
            Object.Destroy(floorObject);
        }

        player = null;
        yield return null;
    }

    [UnityTest]
    public IEnumerator Player_FallsAndStandsOnTerrain()
    {
        SpawnPlayer(new Vector2(5f, 8f));

        yield return Step(180);

        Assert.IsTrue(player.IsGrounded, "Player should be grounded after settling on the floor.");
        Assert.GreaterOrEqual(
            player.transform.position.y,
            FloorTopY - 0.05f,
            "Player must rest on top of the floor, not sink into it.");
        Assert.That(
            player.transform.position.y,
            Is.EqualTo(RestCenterY).Within(0.2f),
            "Resting center should sit ~half the player height above the floor top.");
        Assert.That(player.Velocity.y, Is.EqualTo(0f).Within(0.5f), "Vertical velocity should settle near zero.");
    }

    [UnityTest]
    public IEnumerator Player_JumpsWhenGrounded()
    {
        SpawnPlayer(new Vector2(5f, 8f));
        yield return Step(180);
        Assume.That(player.IsGrounded, "Precondition: player is grounded before jumping.");

        float restY = player.transform.position.y;
        player.RequestJump();

        float maxY = restY;
        for (int i = 0; i < 30; i++)
        {
            yield return new WaitForFixedUpdate();
            maxY = Mathf.Max(maxY, player.transform.position.y);
        }

        Assert.Greater(maxY, restY + 0.5f, "A grounded jump should lift the player clearly above rest height.");

        // And it should come back down and re-ground.
        yield return Step(240);
        Assert.IsTrue(player.IsGrounded, "Player should land and be grounded again after the jump arc.");
    }

    [UnityTest]
    public IEnumerator Player_AirborneJumpRequestIsIgnored()
    {
        // Spawn high so the player is airborne and not grounded.
        SpawnPlayer(new Vector2(5f, 12f));
        yield return new WaitForFixedUpdate();
        Assume.That(player.IsGrounded, Is.False, "Precondition: player is airborne right after spawn.");

        float yBefore = player.transform.position.y;
        float vyBefore = player.Velocity.y;
        player.RequestJump();
        yield return new WaitForFixedUpdate();

        // Falling continues (velocity stays negative / does not jump upward).
        Assert.LessOrEqual(player.Velocity.y, vyBefore + 0.01f, "Airborne jump must not add upward velocity.");
        Assert.Less(player.transform.position.y, yBefore + 0.01f, "Airborne player should keep falling, not rise.");
    }

    [UnityTest]
    public IEnumerator Player_StopsWalkingIntoWall()
    {
        SpawnPlayer(new Vector2(5f, 8f));
        yield return Step(180);
        Assume.That(player.IsGrounded, "Precondition: player grounded before walking.");

        player.SetExternalMoveInput(1f, 5f); // walk right toward the wall at x == WallX.
        yield return Step(240);

        float x = player.transform.position.x;
        Assert.Less(x, (float)WallX, "Player must not pass through the wall's left face.");
        Assert.Greater(x, (float)WallX - 1f, "Player should have actually walked up to the wall, then stopped.");
        Assert.IsTrue(player.IsGrounded, "Player should stay grounded while walking on the floor.");
    }

    [UnityTest]
    public IEnumerator Player_DoesNotTunnelWhileFallingUnderGravity()
    {
        // Drop from a height so the player accelerates under gravity across the whole fall.
        SpawnPlayer(new Vector2(5f, 15f));

        float minCenterY = float.MaxValue;
        for (int i = 0; i < 300; i++)
        {
            yield return new WaitForFixedUpdate();
            minCenterY = Mathf.Min(minCenterY, player.transform.position.y);
            // The collider bottom must stay at the floor surface (allowing only brief soft
            // contact penetration). Real tunneling would drop it a full floor thickness (3) below.
            Assert.GreaterOrEqual(
                player.transform.position.y - PlayerHalfHeight,
                FloorTopY - 0.5f,
                "Player collider bottom tunneled below the floor surface.");
        }

        Assert.IsTrue(player.IsGrounded, "Player should end the fall grounded on the floor.");
        Assert.GreaterOrEqual(minCenterY - PlayerHalfHeight, FloorTopY - 0.5f, "Player must not have passed through the floor.");
    }

    private void BuildFloorAndWall()
    {
        SandboxChunk chunk = new SandboxChunk(Vector2Int.zero);

        for (int y = 0; y < FloorRows; y++)
        {
            for (int x = 0; x < SandboxChunk.Size; x++)
            {
                chunk.SetLocalTile(x, y, new SandboxTile(SandboxTileIds.Stone));
            }
        }

        for (int y = FloorRows; y < WallTopRowExclusive; y++)
        {
            chunk.SetLocalTile(WallX, y, new SandboxTile(SandboxTileIds.Stone));
        }

        floorObject = new GameObject("TestFloor");
        floorObject.transform.position = Vector3.zero;
        SandboxChunkRenderer renderer = floorObject.AddComponent<SandboxChunkRenderer>();
        // Null material is fine: the renderer falls back to a default shader (ResolveMaterial).
        renderer.Rebuild(chunk, TileSize, null);

        Assert.Greater(
            floorObject.GetComponents<BoxCollider2D>().Length,
            0,
            "Floor rebuild should have produced run-merged BoxCollider2D colliders.");
    }

    private void SpawnPlayer(Vector2 position)
    {
        playerObject = new GameObject("TestPlayer");
        playerObject.transform.position = position;

        // Configure the required components BEFORE adding the controller so its Awake picks them up.
        Rigidbody2D body = playerObject.AddComponent<Rigidbody2D>();
        body.gravityScale = 1f;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;

        BoxCollider2D box = playerObject.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.8f, PlayerHalfHeight * 2f);

        player = playerObject.AddComponent<SandboxPlayerController>();
    }

    private static IEnumerator Step(int fixedFrames)
    {
        for (int i = 0; i < fixedFrames; i++)
        {
            yield return new WaitForFixedUpdate();
        }
    }
}
