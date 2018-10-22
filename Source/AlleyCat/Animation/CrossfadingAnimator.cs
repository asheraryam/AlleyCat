using System;
using System.Linq;
using System.Reactive.Linq;
using AlleyCat.Common;
using AlleyCat.Event;
using EnsureThat;
using Godot;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;

namespace AlleyCat.Animation
{
    public class CrossfadingAnimator : AnimationControl, IAnimator
    {
        public Option<Godot.Animation> Animation
        {
            get => _animation.Value;
            set => _animation.Value = value;
        }

        public float Time
        {
            get => TransitionNode.XfadeTime;
            set => TransitionNode.XfadeTime = Mathf.Min(value, 0);
        }

        public IObservable<Option<Godot.Animation>> OnAnimationChange => _animation;

        protected string Parameter { get; }

        protected int Transition => (int) Context.AnimationTree.Get(Parameter);

        protected AnimationNodeTransition TransitionNode { get; }

        protected AnimationNodeAnimation AnimationNode => Transition == 1 ? AnimationNode1 : AnimationNode2;

        protected AnimationNodeAnimation AnimationNode1 { get; }

        protected AnimationNodeAnimation AnimationNode2 { get; }

        private readonly ReactiveProperty<Option<Godot.Animation>> _animation;

        public CrossfadingAnimator(
            string parameter,
            AnimationNodeTransition transitionNode,
            AnimationNodeAnimation animationNode1,
            AnimationNodeAnimation animationNode2,
            AnimationGraphContext context) : base(context)
        {
            Ensure.That(parameter, nameof(parameter)).IsNotNull();
            Ensure.That(transitionNode, nameof(transitionNode)).IsNotNull();
            Ensure.That(animationNode1, nameof(animationNode1)).IsNotNull();
            Ensure.That(animationNode2, nameof(animationNode2)).IsNotNull();
            Ensure.That(context, nameof(context)).IsNotNull();

            Parameter = parameter;

            TransitionNode = transitionNode;
            AnimationNode1 = animationNode1;
            AnimationNode2 = animationNode2;

            var current = AnimationNode.Animation.TrimToOption().Bind(context.Player.FindAnimation);

            _animation = new ReactiveProperty<Option<Godot.Animation>>(current).AddTo(this);

            _animation
                .Select(a => a.Map(context.Player.AddAnimation))
                .Subscribe(animation =>
                {
                    var next = Transition == 1 ? 2 : 1;
                    var node = next == 1 ? AnimationNode1 : AnimationNode2;

                    node.SetAnimation(animation.ValueUnsafe());

                    Context.AnimationTree.Set(Parameter, next);
                })
                .AddTo(this);
        }

        public static Option<CrossfadingAnimator> TryCreate(
            string name,
            IAnimationGraph parent,
            AnimationGraphContext context)
        {
            Ensure.That(name, nameof(name)).IsNotNull();
            Ensure.That(parent, nameof(parent)).IsNotNull();
            Ensure.That(context, nameof(context)).IsNotNull();

            //TODO Resolve in an automatic fashion when it becomes possible to manipulate node connections from code.
            return (from transition in parent.FindAnimationNode<AnimationNodeTransition>(name)
                from animation1 in parent.FindAnimationNode<AnimationNodeAnimation>(name + " Animation 1")
                from animation2 in parent.FindAnimationNode<AnimationNodeAnimation>(name + " Animation 2")
                select (transition, animation1, animation2)).Map(t =>
            {
                var parameter = string.Join("/",
                    new[] {"parameters", parent.Path, name, "current"}.Where(v => v.Length > 0));

                return new CrossfadingAnimator(parameter, t.transition, t.animation1, t.animation2, context);
            });
        }
    }

    public static class CrossfadingAnimatorExtensions
    {
        public static Option<CrossfadingAnimator> FindCrossfadingAnimator(
            this IAnimationGraph graph, string path)
        {
            Ensure.That(graph, nameof(graph)).IsNotNull();
            Ensure.That(path, nameof(path)).IsNotNull();

            return graph.FindDescendantControl<CrossfadingAnimator>(path);
        }
    }
}