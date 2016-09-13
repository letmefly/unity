using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class FSM
{
    public List<Condition> mConditions = new List<Condition>();
    public List<Expression> mExpressions = new List<Expression>();
    private List<State> mStates = new List<State>();
    private State mCurrentState;
    private State mPrevState;
    private State mNextState;
    private float mTimeInState;
    private bool mDebugTag;
    private int mTokenIdx = 0;

    public State AddState(string name)
    {
        State state = new State(name);
        mStates.Add(state);
        if (null == mCurrentState)
        {
            mCurrentState = state;
        }
        return state;
    }

    public void addTransition(string fromStateStr, string toStateStr, string expressionStr)
    {
        int fromStateIndex = GetStateIndex(fromStateStr);
        int toStateIndex = GetStateIndex(toStateStr);
        if (-1 == fromStateIndex || -1 == toStateIndex)
        {
            return;
        }
        State fromState = mStates[fromStateIndex];
        State toState = mStates[toStateIndex];

        List<Token> tokens = new List<Token>();
        if (TokenizeExpression(expressionStr, tokens))
        {
            // build expression from tokens
            mTokenIdx = 0;
            Expression expression = CreateExpression(tokens);
            if ( fromStateIndex == -1 )
            {
                for ( int i = 0; i < mStates.Count; i++ )
                    mStates[i].mTransitions.Add(new State.Transition(expression, toStateIndex));
            }
            else
		    {
                mStates[fromStateIndex].mTransitions.Add(new State.Transition(expression, toStateIndex));
		    }
        }
    }

    public void Begin()
    {
        mCurrentState = mStates[0];
	    mTimeInState = 0.0f;
        if (mCurrentState.mEnter != null)
        {
            mCurrentState.mEnter();
        }
    }

    public void End()
    {
        if (mCurrentState.mExit != null)
        {
            mCurrentState.mExit();
        }
    }

    public void SetCondition(string condition, bool setVal)
    {
        int idx = GetConditionIndex(condition);
        if (idx >= 0)
        {
            mConditions[idx].mSet = setVal;
        }
    }

    public void PulseCondition(string condition)
    {
        int idx = GetConditionIndex(condition);
        if (idx >= 0)
        {
            mConditions[idx].mPulse = true;
        }
    }

    public void ClearAllCondition()
    {
        for (int i = 0; i < mConditions.Count; i++)
        {
            mConditions[i].mPulse = false;
            mConditions[i].mSet = false;
        }
    }

    public void Evaluate()
    {
        int newState = TestExpressions();
        int depth = 0;
        while (newState >= 0 && mStates[newState].mDecisionStatus && depth < 10 )
        {
            HandleTransition(newState);
            newState = TestExpressions();
        }
        for(int i = 0; i < mConditions.Count; i++)
        {
            mConditions[i].mPulse = false;
        }
        if (newState >= 0)
        {
            HandleTransition(newState);
        }
    }

    public void Tick(float dt)
    {
        if (mCurrentState.mTick != null)
        {
            mCurrentState.mTick(dt);
        }
        mTimeInState += dt;
    }

    public void Draw()
    {
        if (mCurrentState.mDraw != null)
        {
            mCurrentState.mDraw();
        }
    }

    private void HandleTransition(int newState)
    {
        mPrevState = mCurrentState;
        mNextState = mStates[newState];

        if (mCurrentState.mExit != null)
        {
            mCurrentState.mExit();
        }
        mCurrentState = mNextState;
        if (mCurrentState.mEnter != null)
        {
            mCurrentState.mEnter();
        }
        mTimeInState = 0.0f;
        if (mDebugTag)
            Debug.Log("transition - " + mPrevState.GetName() + "->" + mNextState.GetName() + "\n");
    }

    private int TestExpressions()
    {
        for (int i = 0; i < mCurrentState.mTransitions.Count; i++)
        {
            if (mCurrentState.mTransitions[i].mExpression.Evaluate(this))
            {
                return mCurrentState.mTransitions[i].mToState;
            }
        }
        return -1;
    }

    private Expression CreateExpression(List<Token> tokens)
    {
        Expression currExpression = null;

        if (mTokenIdx >= tokens.Count)
        {
            return currExpression;
        }

        if (tokens[mTokenIdx].mType == eTokenType.TOK_CONDITION)
        {
            currExpression = new ConditionExpression(tokens[mTokenIdx].mIndex);
            mExpressions.Add(currExpression);
        }
        else if (tokens[mTokenIdx].mType == eTokenType.TOK_OPEN_PAREN)
        {
            mTokenIdx += 1;
            currExpression = CreateExpression(tokens);
        }
        else if (tokens[mTokenIdx].mType == eTokenType.TOK_NOT)
        {
            mTokenIdx += 1;
            Expression childExpression = CreateExpression(tokens);
            Expression expression = new NotExpression(childExpression);
            mExpressions.Add(expression);
            currExpression = expression;
        }

        // Right Expressions
        if (mTokenIdx + 1 >= tokens.Count)
        {
            return currExpression;
        }

        mTokenIdx += 1;
        if (tokens[mTokenIdx].mType == eTokenType.TOK_AND)
        {
            mTokenIdx += 1;
            Expression childA = currExpression;
            Expression childB = CreateExpression(tokens);
            Expression expression = new AndExpression(childA, childB);
            mExpressions.Add(expression);
            currExpression = expression;
        }
        else if (tokens[mTokenIdx].mType == eTokenType.TOK_OR)
        {
            mTokenIdx += 1;
            Expression childA = currExpression;
            Expression childB = CreateExpression(tokens);

            Expression expression = new OrExpression(childA, childB);
            mExpressions.Add(expression);
            currExpression = expression;
        }

        return currExpression;
    }

    private bool TokenizeExpression(string strExpression, List<Token> tokens)
    {
        int i = 0;
        while (i < strExpression.Length)
        {
            if (strExpression[i] == ' ')
            {
                i++;
            }
            else if (strExpression[i] == '&')
            {
                tokens.Add(new Token(eTokenType.TOK_AND));
                i++;
            }
            else if (strExpression[i] == '|')
            {
                tokens.Add(new Token(eTokenType.TOK_OR));
                i++;
            }
            else if (strExpression[i] == '(')
            {
                tokens.Add(new Token(eTokenType.TOK_OPEN_PAREN));
                i++;
            }
            else if (strExpression[i] == ')')
            {
                tokens.Add(new Token(eTokenType.TOK_CLOSE_PAREN));
                i++;
            }
            else if (strExpression[i] == '!')
            {
                tokens.Add(new Token(eTokenType.TOK_NOT));
                i++;
            }
            else if (Isalnum(strExpression[i]))
            {
                // extract condition
                char[] chars = new char[1024];
                int j = 0;
                do
                {
                    chars[j] = strExpression[i];
                    i++;
                    j++;
                } while (i < strExpression.Length && Isalnum(strExpression[i]));
                chars[j] = '\0';
                string strCondition = new string(chars, 0, j);
                int conditionIndex = GetConditionIndex(strCondition);
                if (-1 == conditionIndex)
                {
                    conditionIndex = mConditions.Count;
                    mConditions.Add(new Condition(strCondition));
                }
                Token token = new Token(eTokenType.TOK_CONDITION);
                token.mIndex = conditionIndex;
                tokens.Add(token);
            }
            else
            {
                Debug.LogError("Expression parsing error");
                return false;
            }
        }

        return true;
    }

    private int GetConditionIndex(string strCondition)
    {
        int hashedName = strCondition.GetHashCode();
        for (int i = 0; i < mConditions.Count; i++)
        {
            if (mConditions[i].mHashedName == hashedName)
                return i;
        }
	    return -1;
    }

    private int GetStateIndex(string strState)
    {
        int hashedName = strState.GetHashCode();

	    for ( int i = 0; i < mStates.Count; i++ )
		    if ( mStates[i].mHashedName == hashedName )
			    return i;

	    return -1;
    }

    private Expression FindTrueExpression()
    {
        for (int i = 0; i < mExpressions.Count; i++)
        {
            if (mExpressions[i].mType == Expression.eType.TYPE_TRUE)
            {
                TrueExpression expression = (TrueExpression)mExpressions[i];
                return expression;
            }
        }
        return null;
    }

    private Expression FindNotExpression(Expression child)
    {
        for(int i = 0; i < mExpressions.Count; i++)
        {
            if (mExpressions[i].mType == Expression.eType.TYPE_NOT)
            {
                NotExpression expression = (NotExpression)mExpressions[i];
                if (expression.mChild == child)
                {
                    return expression;
                }
            }
        }
        return null;
    }

    private Expression FindAndExpression(Expression childA, Expression childB)
    {
        for (int i = 0; i < mExpressions.Count; i++)
        {
            if (mExpressions[i].mType == Expression.eType.TYPE_AND)
            {
                AndExpression expression = (AndExpression)mExpressions[i];
                if ((expression.mChildA == childA && expression.mChildB == childB) ||
                    (expression.mChildA == childB && expression.mChildB == childA))
                {
                    return expression;
                }
            }
        }
        return null;
    }

    private Expression FindOrExpression(Expression childA, Expression childB)
    {
        for (int i = 0; i < mExpressions.Count; i++)
        {
            if (mExpressions[i].mType == Expression.eType.TYPE_OR)
            {
                AndExpression expression = (AndExpression)mExpressions[i];
                if ((expression.mChildA == childA && expression.mChildB == childB) ||
                    (expression.mChildA == childB && expression.mChildB == childA))
                {
                    return expression;
                }
            }
        }
        return null;
    }

    private bool Isalnum(char c)
    {
        if ((c > 57 || c < 48) && c != '_' && (c < 65 || c > 90) && (c < 97 || c > 122))
        {
            return false;
        }
        return true;
    }

    // ------------------------ State Class Define ------------------------
    public delegate void StateEnter();
    public delegate void StateExit();
    public delegate void StateTick(float fdt);
    public delegate void StateDraw();
    public delegate void TickMethod(float dt);
    public delegate void TransMethod();
    public delegate void DrawMethod();

    public class State
    {
        public class Transition
	    {
		    public Transition(Expression expression, int toState)
            {
                mExpression = expression;
                mToState = toState;
            }
		    public Expression mExpression;
		    public int mToState;
	    };
        public string mName;
        public int mHashedName;
        public List<Transition> mTransitions = new List<Transition>();
        public StateEnter mEnter;
        public StateExit  mExit;
        public StateTick  mTick;
        public StateDraw  mDraw;
        public bool       mDecisionStatus = false;

        public State(string name)
        {
            mName = name;
            mHashedName = name.GetHashCode();
        }

        public string GetName()
        {
            return mName;
        }

        public void SetEnterMethod(StateEnter enter)
        {
            mEnter = enter;
        }

        public void SetExitMethod(StateExit exit)
        {
            mExit = exit;
        }

        public void SetTickMethod(StateTick tick)
        {
            mTick = tick;
        }

        public void makeDecisionStatus()
        {
            mDecisionStatus = true; 
        }
    }

    // ------------------------ Condition Class Define ------------------------
	public class Condition
    {
        public Condition(string name)
        {
            mHashedName = name.GetHashCode();
        }
        public int mHashedName;
	    public bool mSet = false;
	    public bool mPulse = false;
    }

    // ------------------------ Expression Class Define ------------------------
	public abstract class Expression
    {
        public enum eType { TYPE_TRUE, TYPE_NOT, TYPE_CONDITION, TYPE_AND, TYPE_OR };
        public Expression() { }
        public Expression(eType type)
        {
            mType = type;
        }
        public abstract bool Evaluate(FSM fsm);
        public eType mType;
    }

    // ------------------------ TrueExpression Class Define ------------------------
    public class TrueExpression : Expression
    {
        public TrueExpression():base(eType.TYPE_TRUE) {}
        public override bool Evaluate(FSM fsm)
        {
            return true;
        }
    }

    // ------------------------ NotExpression Class Define ------------------------
    public class NotExpression : Expression
    {
        public NotExpression(Expression childExpression):base(eType.TYPE_NOT)
        {
            mChild = childExpression;
        }
        public override bool Evaluate(FSM fsm)
        {
            return !mChild.Evaluate(fsm);
        }
        public Expression mChild;
    }

    // ------------------------ ConditionExpression Class Define ------------------------
	public class ConditionExpression : Expression
    {
        public ConditionExpression(int condition):base(eType.TYPE_CONDITION)
        {
            mCondition = condition;
        }
        public override bool Evaluate(FSM fsm)
        {
            Condition condition = fsm.mConditions[mCondition];
            return condition.mSet || condition.mPulse;
        }
        public int mCondition;
    }

    // ------------------------ AndExpression Class Define ------------------------
	public class AndExpression : Expression
    {
        public AndExpression(Expression childA, Expression childB) :base(eType.TYPE_AND)
        {
            mChildA = childA;
            mChildB = childB;
        }
        public override bool Evaluate(FSM fsm)
        {
            return mChildA.Evaluate(fsm) && mChildB.Evaluate(fsm);
        }
        public Expression mChildA;
        public Expression mChildB;
    }

    // ------------------------ OrExpression Class Define ------------------------
    public class OrExpression : Expression
    {
        public OrExpression(Expression childA, Expression childB) :base(eType.TYPE_OR)
        {
            mChildA = childA;
            mChildB = childB;
        }
        public override bool Evaluate(FSM fsm)
        {
            return mChildA.Evaluate(fsm) || mChildB.Evaluate(fsm);
        }
        public Expression mChildA;
        public Expression mChildB;
    }

    // ------------------------ Token Class Define ------------------------
    public enum eTokenType { TOK_CONDITION, TOK_AND, TOK_OR, TOK_OPEN_PAREN, TOK_CLOSE_PAREN, TOK_NOT, TOK_END };
    public class Token
    {
        public Token(eTokenType type)
        {
            mType = type;
        }
        public eTokenType mType;
        public int mIndex;
    }
}
