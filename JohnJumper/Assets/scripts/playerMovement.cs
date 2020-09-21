﻿using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;

public class playerMovement : MonoBehaviour
{
    public Sprite wallhang;
    public Sprite midair;
    public Sprite standhang;
    public Sprite stagger;
    public Sprite death;

    public int spikeBumps = 0; // how many times the player "bumped" on a spike
    public float speed = 5f;
    public float jumpSpeed = 700f;
    public float maxJumpSpeed = 2680f;
    public float jumpCooldown = 0.5f;
    public bool isFacingRight = true;
    public bool inPlay = true;
    public bool isMidAir = false;
    public bool isWallSliding = false;
    public float playerStartX = -7.357f;
    public float playerStartY = 15.12f;
    public ComboDisplay player_combo_script;

    private float oldPlayYPosition;
    private float minJumpSpeed;
    private bool isJumping = false;
    private float jumpCooldownC = 0f;
    private float groundDeathOffset = 1.8f;
    private int numSpikesUntilTumble = 2;
    private float timeUntilWallslideParticles = 0.3f;
    private float currentTimeUntilWallSlideParticles = 0f;
    private float timeComboDecays = 2f;
    private float currentTimeBetweenJumps;
    private EchoEffect player_echo;
    private LoopAround player_looparound;
    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform tr;
    private ParticleSystem player_pr;
    private ParticleSystem.EmissionModule player_pr_em;
    private ParticleSystem.ShapeModule player_pr_sp;

    // Start is called before the first frame update
    void Start()
    {
        tr = GetComponent<Transform>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        player_pr = GetComponent<ParticleSystem>();
        player_echo = GetComponent<EchoEffect>();
        player_looparound = GetComponent<LoopAround>();
        player_pr_em = player_pr.emission;
        player_pr_sp = player_pr.shape;
        currentTimeBetweenJumps = timeComboDecays;
        minJumpSpeed = jumpSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (inPlay)
        {
            if (Input.GetButtonDown("Jump") && !isMidAir && jumpCooldownC <= 0f)
            {
                // Wall jump successful
                if (transform.position.y >= oldPlayYPosition) {
                    // If we jump and we move up, increment jump combo by 1.
                    player_combo_script.numCombos = player_combo_script.numCombos == 99 ? 99 : player_combo_script.numCombos+1;
                    currentTimeBetweenJumps = timeComboDecays;
                }
                isJumping = true;
                jumpCooldownC = jumpCooldown;
            }
            if (jumpCooldownC > 0f)
            {
                // Cannot walljump, cooldown in effect
                jumpCooldownC -= Time.deltaTime;
            }
            player_pr.transform.position = new Vector3(transform.position.x, transform.position.y, player_pr.transform.position.z);
            if (currentTimeBetweenJumps > 0) currentTimeBetweenJumps -= Time.deltaTime;
            else {
                player_combo_script.numCombos = 0;
                jumpSpeed = minJumpSpeed;
            }
            oldPlayYPosition = transform.position.y;
        }
        else if (isMidAir) {
            // Rotate the character (tumbling) if spikebumps >= 3;
            if (spikeBumps >= numSpikesUntilTumble) {
                tr.Rotate(0, 0, 1.0f, Space.Self);
            }
        }
    }

    // Fixed update is called after a set time
    void FixedUpdate()
    {
        if (inPlay)
        {
            if (isJumping)
            {
                isJumping = false;
                // jump in the direction character is facing
                if (isFacingRight)
                {
                    rb.AddForce(new Vector2(speed, 0), ForceMode2D.Impulse);
                    rb.AddForce(new Vector2(0, jumpSpeed), ForceMode2D.Force);
                }   else
                {
                    rb.AddForce(new Vector2(-speed, 0), ForceMode2D.Impulse);
                    rb.AddForce(new Vector2(0, jumpSpeed), ForceMode2D.Force);
                }
                if (jumpSpeed < maxJumpSpeed)  jumpSpeed += 20f;
            }
        }
    }


    // Called whenever a collider begins colliding with an object
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.tag == "Ground")
        {
            // Character "dies" and game is over
            inPlay = false;
            isMidAir = false;
            sr.sprite = death;
            rb.isKinematic = true;
            rb.simulated = false;
            tr.position -= new Vector3(0, groundDeathOffset);
            tr.rotation = Quaternion.identity;
            player_pr.Stop();
        } else if (inPlay)
        {
            if (collision.collider.tag == "Wall")
            {
                // Successfully made it to a wall
                isMidAir = false;
                sr.sprite = wallhang;
                if (collision.collider.name == "LeftWall")
                {
                    isFacingRight = true;
                    sr.flipX = false;
                }
                else
                {
                    isFacingRight = false;
                    sr.flipX = true;
                }
                if (rb.velocity.y > 0f)
                {
                    rb.velocity = new Vector3(rb.velocity.x, 0f);
                }
            }
        }
    }


    // Called whenever a collider is currently colliding with an object
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (inPlay)
        {
            if (currentTimeUntilWallSlideParticles < timeUntilWallslideParticles)
            {
                // We just stuck the landing, don't emit wallslide dust
                currentTimeUntilWallSlideParticles += Time.deltaTime;
            } else
            {
                // Emit wallslide dust from wallsliding
                if (!isWallSliding) {
                    // only play this once while sliding
                    if (isFacingRight) {
                        player_pr_sp.position = new Vector3(-0.35f, -0.8f, 0f);
                    } else {
                        player_pr_sp.position = new Vector3(0.35f, -0.8f, 0f);
                    }
                    isWallSliding = true;
                    player_pr.Play();
                    player_pr_em.enabled = true;
                }
            }
        }
    }

    // Called whenever a collider stops colliding with an object
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (inPlay)
        {
            // Stop emitting particles (if emitting at all), and change sprites
            currentTimeUntilWallSlideParticles = 0f;
            isWallSliding = false;
            player_pr.Stop();
            if (collision.collider.tag == "Wall")
            {
                sr.sprite = midair;
                isMidAir = true;
            }
        }   
    }

    public void ResetCharacter() {
        inPlay = true;
        isFacingRight = true;
        sr.flipX = false;
        isWallSliding = true;
        sr.sprite = wallhang;
        tr.position = new Vector3(playerStartX, playerStartY);
        tr.rotation = Quaternion.identity;
        rb.isKinematic = false;
        rb.simulated = true;
        rb.velocity = new Vector3(0f, 0f);
        isJumping = false;
        isMidAir = false;
        player_echo.enabled = true;
        jumpSpeed = minJumpSpeed;
        player_looparound.numLoops = 0;
        player_combo_script.numCombos = 0;
        spikeBumps = 0;
    }
}
