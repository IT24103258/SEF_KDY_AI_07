import 'package:flutter/material.dart';

import '../core/errors/app_exception.dart';
import '../core/network/api_client.dart';
import '../models/agent_workflow_model.dart';

/// Loads the real `AgentWorkflow` / `AgentStep` records the backend persisted
/// for a request and renders them as a timeline.
///
/// This is a view over recorded state, not a live execution stream: the Python
/// agent run has already finished by the time the workflow row exists. Only the
/// statuses, timestamps and tool-call timings the backend actually stored are
/// shown — no stage is invented and nothing is animated as "executing" unless
/// the backend recorded that step as `Running`.
class AgentTimelineWidget extends StatefulWidget {
  final String requestId;

  const AgentTimelineWidget({super.key, required this.requestId});

  @override
  State<AgentTimelineWidget> createState() => _AgentTimelineWidgetState();
}

class _AgentTimelineWidgetState extends State<AgentTimelineWidget> {
  final ApiClient _apiClient = ApiClient();

  bool _isLoading = true;
  String? _error;
  AgentWorkflowModel? _workflow;

  @override
  void initState() {
    super.initState();
    _load();
  }

  @override
  void didUpdateWidget(covariant AgentTimelineWidget oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.requestId != widget.requestId) _load();
  }

  Future<void> _load() async {
    setState(() {
      _isLoading = true;
      _error = null;
    });

    try {
      final response = await _apiClient.get('/agentworkflows');
      final data = response['data'];
      final items = data is List ? data : const <dynamic>[];

      AgentWorkflowModel? match;
      final wanted = widget.requestId.toLowerCase();
      for (final raw in items) {
        if (raw is! Map<String, dynamic>) continue;
        final workflow = AgentWorkflowModel.fromJson(raw);
        if (workflow.requestId.toLowerCase() == wanted) {
          // The backend returns the list newest-first, so the first match is
          // the most recent recorded run for this request.
          match = workflow;
          break;
        }
      }

      if (!mounted) return;
      setState(() {
        _workflow = match;
        _isLoading = false;
      });
    } on AppException catch (exception) {
      if (!mounted) return;
      setState(() {
        _error = exception.message;
        _isLoading = false;
      });
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _error = 'Could not load the agent workflow timeline.';
        _isLoading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(10),
        side: BorderSide(color: Colors.grey.withValues(alpha: 0.25)),
      ),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(12, 12, 12, 12),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                const Icon(Icons.timeline_outlined, size: 16, color: Colors.grey),
                const SizedBox(width: 6),
                const Expanded(
                  child: Text(
                    'AGENT PROCESSING TIMELINE',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 11,
                      letterSpacing: 0.6,
                      color: Colors.grey,
                    ),
                  ),
                ),
                IconButton(
                  visualDensity: VisualDensity.compact,
                  tooltip: 'Reload timeline',
                  onPressed: _isLoading ? null : _load,
                  icon: const Icon(Icons.refresh, size: 18, color: Colors.grey),
                  padding: EdgeInsets.zero,
                  constraints: const BoxConstraints(),
                ),
              ],
            ),
            const SizedBox(height: 2),
            if (_isLoading)
              const Padding(
                padding: EdgeInsets.symmetric(vertical: 18),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (_error != null)
              _buildMessage(
                icon: Icons.warning_amber_rounded,
                color: Colors.red,
                text: _error!,
              )
            else if (_workflow == null)
              _buildMessage(
                icon: Icons.inbox_outlined,
                color: Colors.grey,
                text: 'No agent workflow recorded for this request.',
              )
            else
              AgentWorkflowTimelineView(workflow: _workflow!),
          ],
        ),
      ),
    );
  }

  Widget _buildMessage({
    required IconData icon,
    required Color color,
    required String text,
  }) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 10),
      child: Row(
        children: [
          Icon(icon, color: color, size: 20),
          const SizedBox(width: 10),
          Expanded(
            child: Text(
              text,
              style: TextStyle(fontSize: 11.5, color: color),
            ),
          ),
        ],
      ),
    );
  }
}

/// Pure rendering half of the timeline. Split out so the display can be
/// verified against real workflow data without performing a network call.
class AgentWorkflowTimelineView extends StatelessWidget {
  final AgentWorkflowModel workflow;

  const AgentWorkflowTimelineView({super.key, required this.workflow});

  @override
  Widget build(BuildContext context) {
    final workflowStyle = WorkflowStatusStyle.of(workflow.status);

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Container(
          padding: const EdgeInsets.symmetric(horizontal: 9, vertical: 7),
          decoration: BoxDecoration(
            color: workflowStyle.color.withValues(alpha: 0.1),
            borderRadius: BorderRadius.circular(8),
            border: Border.all(color: workflowStyle.color.withValues(alpha: 0.35)),
          ),
          child: Row(
            children: [
              Icon(workflowStyle.icon, size: 15, color: workflowStyle.color),
              const SizedBox(width: 7),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(
                      'Workflow: ${workflow.workflowType.isEmpty ? 'Unknown' : workflow.workflowType}',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        fontSize: 12,
                      ),
                    ),
                    const SizedBox(height: 1),
                    Text(
                      'Status: ${workflowStyle.label}',
                      style: TextStyle(
                        fontSize: 11,
                        color: workflowStyle.color,
                        fontWeight: FontWeight.w600,
                      ),
                    ),
                    if (workflow.recordedAtText != null) ...[
                      const SizedBox(height: 1),
                      Text(
                        'Recorded: ${workflow.recordedAtText}',
                        style: const TextStyle(fontSize: 10, color: Colors.grey),
                      ),
                    ],
                  ],
                ),
              ),
            ],
          ),
        ),

        const SizedBox(height: 12),

        if (workflow.steps.isEmpty)
          const Text(
            'The backend recorded this workflow without any agent steps.',
            style: TextStyle(fontSize: 11.5, color: Colors.grey),
          )
        else
          Column(
            children: [
              for (var index = 0; index < workflow.steps.length; index++)
                _StepRow(
                  step: workflow.steps[index],
                  isLast: index == workflow.steps.length - 1,
                ),
            ],
          ),

        const SizedBox(height: 10),
        const Text(
          'Recorded workflow state persisted by the backend. This is not a live '
          'execution stream and no step is shown as running unless the backend '
          'recorded it that way.',
          style: TextStyle(fontSize: 10, color: Colors.grey),
        ),
      ],
    );
  }
}

class _StepRow extends StatelessWidget {
  final AgentStepModel step;
  final bool isLast;

  const _StepRow({required this.step, required this.isLast});

  @override
  Widget build(BuildContext context) {
    final style = WorkflowStatusStyle.of(step.status);
    final running = style.isRunning;

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Column(
            children: [
              Container(
                width: 24,
                height: 24,
                alignment: Alignment.center,
                decoration: BoxDecoration(
                  shape: BoxShape.circle,
                  color: style.color.withValues(alpha: 0.12),
                  border: Border.all(color: style.color),
                ),
                child: running
                    ? const SizedBox(
                        width: 12,
                        height: 12,
                        child: CircularProgressIndicator(strokeWidth: 2),
                      )
                    : Icon(style.icon, size: 13, color: style.color),
              ),
              if (!isLast)
                Expanded(
                  child: Container(
                    width: 2,
                    margin: const EdgeInsets.symmetric(vertical: 2),
                    color: Colors.grey.withValues(alpha: 0.3),
                  ),
                ),
            ],
          ),
          const SizedBox(width: 10),
          Expanded(
            child: Padding(
              padding: EdgeInsets.only(bottom: isLast ? 0 : 14),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Wrap(
                    spacing: 6,
                    runSpacing: 4,
                    crossAxisAlignment: WrapCrossAlignment.center,
                    children: [
                      Text(
                        step.agentName.isEmpty ? 'Unknown agent' : step.agentName,
                        style: const TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 12,
                        ),
                      ),
                      Container(
                        padding: const EdgeInsets.symmetric(
                          horizontal: 6,
                          vertical: 2,
                        ),
                        decoration: BoxDecoration(
                          color: style.color.withValues(alpha: 0.14),
                          borderRadius: BorderRadius.circular(10),
                        ),
                        child: Text(
                          style.label,
                          style: TextStyle(
                            fontSize: 9.5,
                            fontWeight: FontWeight.bold,
                            color: style.color,
                          ),
                        ),
                      ),
                    ],
                  ),
                  if (step.stepName.isNotEmpty) ...[
                    const SizedBox(height: 1),
                    Text(
                      step.stepName,
                      style: const TextStyle(fontSize: 11, color: Colors.black54),
                    ),
                  ],
                  if (step.recordedAtText != null) ...[
                    const SizedBox(height: 1),
                    Text(
                      'Recorded: ${step.recordedAtText}',
                      style: const TextStyle(fontSize: 10, color: Colors.grey),
                    ),
                  ],
                  const SizedBox(height: 4),
                  _ToolCallList(toolCalls: step.toolCalls),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _ToolCallList extends StatelessWidget {
  final List<AgentToolCallModel> toolCalls;

  const _ToolCallList({required this.toolCalls});

  @override
  Widget build(BuildContext context) {
    if (toolCalls.isEmpty) {
      return const Text(
        'No tool calls recorded for this step.',
        style: TextStyle(fontSize: 10.5, color: Colors.grey),
      );
    }

    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        for (final call in toolCalls)
          Padding(
            padding: const EdgeInsets.only(bottom: 2),
            child: Row(
              children: [
                Icon(
                  call.success ? Icons.check_circle_outline : Icons.error_outline,
                  size: 12,
                  color: call.success ? const Color(0xFF10B981) : Colors.red,
                ),
                const SizedBox(width: 5),
                Expanded(
                  child: Text(
                    call.toolName.isEmpty ? 'Unknown tool' : call.toolName,
                    style: const TextStyle(fontSize: 10.5),
                  ),
                ),
                Text(
                  '${call.executionTimeMs} ms',
                  style: const TextStyle(fontSize: 10, color: Colors.grey),
                ),
              ],
            ),
          ),
      ],
    );
  }
}

/// Maps the backend `WorkflowStatus` strings onto display styling.
///
/// Unknown values keep their raw text so the UI never claims a state the
/// backend did not record.
class WorkflowStatusStyle {
  final String label;
  final Color color;
  final IconData icon;
  final bool isRunning;

  const WorkflowStatusStyle({
    required this.label,
    required this.color,
    required this.icon,
    this.isRunning = false,
  });

  factory WorkflowStatusStyle.of(String status) {
    final normalized = status.trim().toUpperCase().replaceAll('_', '');
    switch (normalized) {
      case 'COMPLETED':
        return const WorkflowStatusStyle(
          label: 'COMPLETED',
          color: Color(0xFF10B981),
          icon: Icons.check_circle,
        );
      case 'FAILED':
        return const WorkflowStatusStyle(
          label: 'FAILED',
          color: Colors.red,
          icon: Icons.error,
        );
      case 'WAITINGFORAPPROVAL':
        return WorkflowStatusStyle(
          label: 'WAITING FOR APPROVAL',
          color: Colors.amber.shade700,
          icon: Icons.person_pin_circle_outlined,
        );
      case 'APPROVED':
        return const WorkflowStatusStyle(
          label: 'APPROVED',
          color: Color(0xFF10B981),
          icon: Icons.verified,
        );
      case 'REJECTED':
        return const WorkflowStatusStyle(
          label: 'REJECTED',
          color: Colors.red,
          icon: Icons.block,
        );
      case 'RUNNING':
        return const WorkflowStatusStyle(
          label: 'RUNNING',
          color: Color(0xFF2563EB),
          icon: Icons.autorenew,
          isRunning: true,
        );
      case 'PENDING':
        return const WorkflowStatusStyle(
          label: 'PENDING',
          color: Colors.grey,
          icon: Icons.schedule,
        );
      default:
        return WorkflowStatusStyle(
          label: humanize(status),
          color: Colors.grey,
          icon: Icons.help_outline,
        );
    }
  }

  static String humanize(String status) {
    final trimmed = status.trim().replaceAll('_', ' ');
    if (trimmed.isEmpty) return 'UNKNOWN';
    final spaced = trimmed.replaceAllMapped(
      RegExp(r'(?<=[a-z0-9])(?=[A-Z])'),
      (match) => ' ',
    );
    return spaced.toUpperCase();
  }
}
