using UnityEngine;

// Maquina de Estados Finitos para um inimigo de tower defense.
// Estados: Walk -> Retreat -> Die.
public class IABehavior : MonoBehaviour
{
    public enum State
    {
        Walk,
        Retreat,
        Die,
        SpeedUp,
        Back
    }

    [Header("Settings")]
    public GameObject target;
    public float speed = 2f;
    public float speedUpMultiplier =2.5f;
    public float retreatSpeed = 3f;
    public float retreatDuration = 0.5f;
    public float backSpeed = 5f;
    public float backDuration = 1.5f;
    public int health = 1;

    private State currentState;
    private Rigidbody body;
    private float retreatTimer;
    private float backTimer;

    void Start()
    {
        body = GetComponent<Rigidbody>();

        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player;
            }
        }

        ChangeState(State.Walk);
    }

    void FixedUpdate()
    {
        switch (currentState)
        {
            case State.Walk:
                TickWalk();
                break;
            case State.Retreat:
                TickRetreat();
                break;
            case State.Die:
                TickDie();
                break;
            case State.SpeedUp:
                TickSpeedUp();
                break;
            case State.Back:
                TickBack();
                break;
        }
    }

    void TickWalk()
    {
        if (target == null)
        {
            return;
        }

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        body.linearVelocity = direction.normalized * speed;
    }

    void TickRetreat()
    {
        if (target == null)
        {
            ChangeState(State.Walk);
            return;
        }

        Vector3 direction = transform.position - target.transform.position;
        direction.y = 0f;
        body.linearVelocity = direction.normalized * retreatSpeed;
        retreatTimer += Time.fixedDeltaTime;

        if (retreatTimer >= retreatDuration)
        {
            ChangeState(State.Walk);
        }
    }

    void TickDie()
    {
        Destroy(gameObject);
    }
    
    void TickSpeedUp()
    {
        if (target == null)
        {
            ChangeState(State.Walk);
            return;
        }

        Vector3 direction = target.transform.position - transform.position;
        direction.y = 0f;
        body.linearVelocity = direction.normalized * (speed * speedUpMultiplier);
       
    }

    void TickBack()
    {
       
            if (target == null)
            {
                ChangeState(State.Walk);
                return;
            }

            Vector3 direction = transform.position - target.transform.position;
            direction.y = 0f;
            body.linearVelocity = direction.normalized * backSpeed;
            backTimer += Time.fixedDeltaTime;

            if (backTimer >= backDuration)
            {
                
                if (health <= 2)
                {
                    ChangeState(State.SpeedUp);
                }
                else
                {
                    ChangeState(State.Walk);
                }
            }
        
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 0)
        {
            ChangeState(State.Die);
        }
        else if (health == 3 && currentState != State.Back)
        {
           
            ChangeState(State.Back);
        }
        else if (health <= 2 && currentState != State.SpeedUp && currentState != State.Back && currentState != State.Retreat)
        {
           
            ChangeState(State.SpeedUp);
        }
    }

    public void ChangeState(State newState)
    {
        currentState = newState;

        if (newState == State.Retreat)
        {
            retreatTimer = 0f;
        }
        else if (newState == State.Back)
        {
            backTimer = 0f; // Reseta o tempo de recuo ao entrar no estado Back
        }

        Debug.Log(gameObject.name + " changed to: " + newState);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject == target)
        {
            ChangeState(State.Retreat);
        }
    }

    void OnParticleCollision(GameObject other)
    {
        TakeDamage(1);
    }

    // TAREFA: adicione 2 estados novos a maquina de estados desta IA.
    // Sugestoes:
    // 1) Stunned - o inimigo fica parado por um tempo apos levar dano
    //    (usar um timer parecido com retreatTimer e voltar para Walk depois).
    // 2) Fast - quando a vida estiver baixa (ex: health == 1) o inimigo
    //    aumenta a velocidade para tentar chegar mais rapido na base.
    // Lembre-se de:
    // - adicionar o novo valor no enum State
    // - criar o metodo TickNomeDoEstado()
    // - adicionar o case no switch dentro de FixedUpdate()
    // - definir quando o estado deve iniciar e quando deve terminar
}
