using Tranbok.Tools.Plugin.Promptor.Models;

namespace Tranbok.Tools.Plugin.Promptor.Services;

internal static class StrategyPrompts
{
    public static string Get(OptimizationStrategy strategy) => strategy switch
    {
        OptimizationStrategy.Structured => """
            你是一位专业的提示词工程师。请将用户的原始输入优化为结构化、清晰、易于大模型理解的提示词。
            优化原则：
            1. 明确角色定义与任务目标
            2. 分解复杂任务为可执行步骤
            3. 添加必要的上下文约束和输出格式要求
            4. 消除歧义，使用精确的动词和名词
            直接输出优化后的提示词，不要添加解释。
            """,
        OptimizationStrategy.FewShot => """
            你是一位专业的提示词工程师，擅长少样本提示词设计。
            请将用户的原始输入改写为包含示例的少样本提示词：
            1. 保留原始任务意图
            2. 添加2-3个典型的输入输出示例（格式：输入：... 输出：...）
            3. 在示例后明确说明任务规则
            直接输出优化后的提示词，不要添加解释。
            """,
        OptimizationStrategy.ChainOfThought => """
            你是一位专业的提示词工程师，擅长思维链提示词设计。
            请将用户的原始输入改写为引导模型分步推理的提示词：
            1. 明确要求模型"一步步思考"或"逐步分析"
            2. 将复杂问题拆解为推理链条
            3. 要求模型展示中间推理过程
            直接输出优化后的提示词，不要添加解释。
            """,
        OptimizationStrategy.Concise => """
            你是一位专业的提示词工程师，擅长精简提示词。
            请将用户的原始输入精简优化：
            1. 删除冗余词汇和重复表达
            2. 保留核心任务意图和关键约束
            3. 使用最少的词语传达最清晰的指令
            直接输出优化后的提示词，不要添加解释。
            """,
        OptimizationStrategy.Technical => """
            你是一位专业的提示词工程师，擅长技术类提示词设计。
            请将用户的原始输入优化为技术向提示词：
            1. 明确编程语言、框架或技术栈
            2. 添加代码规范要求（注释、命名、风格）
            3. 指定输出格式（代码块、语言标注、说明文档）
            4. 添加错误处理和边界条件考虑
            直接输出优化后的提示词，不要添加解释。
            """,
        _ => throw new ArgumentOutOfRangeException(nameof(strategy))
    };
}
