import 'package:flutter/material.dart';

import '../models/priority_assessment_model.dart';

/// Explains how the authoritative Component 2 result was formed.
///
/// Every value rendered here comes straight from the persisted, C#-validated
/// assessment (`PriorityAssessmentDto`). Nothing is recalculated, re-scored or
/// guessed: a factor the backend did not report is shown as unavailable.
class ExplainableAssessmentWidget extends StatelessWidget {
  final PriorityAssessmentModel assessment;

  const ExplainableAssessmentWidget({super.key, required this.assessment});

  static const String _unavailable = 'Unavailable';

  bool get _failed => assessment.status.toUpperCase() == 'FAILED';

  ContributingFactors? get _factors => assessment.contributingFactors;

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
            const Row(
              children: [
                Icon(Icons.psychology_outlined, size: 16, color: Colors.grey),
                SizedBox(width: 6),
                Expanded(
                  child: Text(
                    'EXPLAINABLE RISK & PRIORITY ASSESSMENT',
                    style: TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 11,
                      letterSpacing: 0.6,
                      color: Colors.grey,
                    ),
                  ),
                ),
              ],
            ),
            const SizedBox(height: 4),
            const Text(
              'All values below are the validated result persisted by the '
              'backend. The app does not recompute them.',
              style: TextStyle(fontSize: 10.5, color: Colors.grey),
            ),
            const SizedBox(height: 10),

            if (_failed) ...[
              _buildFailedNote(),
              const SizedBox(height: 10),
            ],

            _buildSectionTitle('Factors Considered'),
            const SizedBox(height: 6),
            _buildFactorRows(),

            const SizedBox(height: 14),
            _buildSectionTitle('Decision Chain'),
            const SizedBox(height: 8),
            _buildDecisionChain(),

            const SizedBox(height: 12),
            Container(
              width: double.infinity,
              padding: const EdgeInsets.all(9),
              decoration: BoxDecoration(
                color: Colors.blue.withValues(alpha: 0.06),
                borderRadius: BorderRadius.circular(8),
                border: Border.all(color: Colors.blue.withValues(alpha: 0.2)),
              ),
              child: const Text(
                'Risk and Priority are separate outputs. The risk score measures '
                'how severe the hazard is; the priority drives technician '
                'dispatch and the SLA response window. A risk score is not a '
                'priority.',
                style: TextStyle(fontSize: 11, color: Colors.blueGrey),
              ),
            ),

            const SizedBox(height: 12),
            _buildSectionTitle('Agent Explanation / Decision Audit'),
            const SizedBox(height: 4),
            Text(
              _failed
                  ? _unavailable
                  : (assessment.explanation.trim().isEmpty
                      ? _unavailable
                      : assessment.explanation),
              style: const TextStyle(fontSize: 11.5, height: 1.35),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildFailedNote() {
    return Container(
      width: double.infinity,
      padding: const EdgeInsets.all(9),
      decoration: BoxDecoration(
        color: Colors.red.withValues(alpha: 0.08),
        borderRadius: BorderRadius.circular(8),
        border: Border.all(color: Colors.red.withValues(alpha: 0.3)),
      ),
      child: const Text(
        'This assessment failed safely, so the agent produced no validated risk '
        'score, level or priority. The values below are reported as unavailable '
        'rather than estimated.',
        style: TextStyle(
          fontSize: 11,
          color: Colors.red,
          fontWeight: FontWeight.w600,
        ),
      ),
    );
  }

  Widget _buildSectionTitle(String title) {
    return Text(
      title,
      style: const TextStyle(
        fontWeight: FontWeight.bold,
        fontSize: 12.5,
      ),
    );
  }

  Widget _buildFactorRows() {
    final factors = _factors;

    return Column(
      children: [
        _FactorRow(
          label: 'Asset Criticality',
          value: _text(factors?.assetCriticality ?? assessment.assetCriticality),
        ),
        _FactorRow(label: 'Impact Level', value: _text(assessment.impactLevel)),
        _FactorRow(
          label: 'Likelihood Level',
          value: _text(assessment.likelihoodLevel),
        ),
        _FactorRow(
          label: 'Base Matrix Score',
          value: _integer(factors?.baseMatrixScore),
        ),
        _FactorRow(
          label: 'Asset Criticality Score',
          value: _integer(factors?.assetCriticalityScore),
        ),
        _FactorRow(label: 'Impact Score', value: _integer(factors?.impactScore)),
        _FactorRow(
          label: 'Likelihood Score',
          value: _integer(factors?.likelihoodScore),
        ),
        _FactorRow(
          label: 'Safety Hazard Detected',
          value: _boolean(factors?.hasSafetyHazard),
        ),
        _FactorRow(
          label: 'Safety Hazard Modifier',
          value: _integer(factors?.safetyHazardModifier),
        ),
        _FactorRow(
          label: 'Recent Failure Count',
          value: _integer(factors?.recentFailureCount),
        ),
        _FactorRow(
          label: 'Recurrence Modifier',
          value: _integer(factors?.recurrenceModifier),
        ),
        _FactorRow(
          label: 'Location Modifier',
          value: _integer(factors?.locationModifier),
        ),
        _FactorRow(
          label: 'Operational Disruption',
          value: _text(factors?.operationalDisruption),
          isLast: true,
        ),
      ],
    );
  }

  Widget _buildDecisionChain() {
    final nodes = <_ChainNode>[
      _ChainNode(
        title: 'Assessment Factors',
        detail: 'Criticality ${_text(assessment.assetCriticality)} '
            '| Impact ${_text(assessment.impactLevel)} '
            '| Likelihood ${_text(assessment.likelihoodLevel)}',
      ),
      _ChainNode(
        title: 'Validated Risk Assessment',
        detail: 'Persisted status: ${_text(assessment.status)}',
      ),
      _ChainNode(
        title: 'Risk Score',
        detail: _failed ? _unavailable : '${assessment.riskScore} / 100',
      ),
      _ChainNode(
        title: 'Risk Level',
        detail: _failed ? _unavailable : _text(assessment.riskLevel),
      ),
      _ChainNode(
        title: 'Priority',
        detail: _failed ? _unavailable : _text(assessment.priority),
      ),
      _ChainNode(
        title: 'Existing SLA / Response',
        detail: '${_text(assessment.recommendedResponseWindow)} '
            '| ${assessment.responseTimeHours}h dispatch '
            '| ${assessment.resolutionTimeHours}h resolution',
      ),
      _ChainNode(
        title: 'Escalation',
        detail: assessment.escalationFlag
            ? 'Escalated: ${_text(assessment.escalationReason)}'
            : 'Not escalated',
        isLast: true,
      ),
    ];

    return Column(
      children: nodes
          .map(
            (node) => _DecisionChainRow(
              node: node,
              isLast: node.isLast,
            ),
          )
          .toList(),
    );
  }

  String _text(String? value) =>
      (value == null || value.trim().isEmpty) ? _unavailable : value.trim();

  String _integer(int? value) => value == null ? _unavailable : '$value';

  String _boolean(bool? value) {
    if (value == null) return _unavailable;
    return value ? 'Yes' : 'No';
  }
}

class _ChainNode {
  final String title;
  final String detail;
  final bool isLast;

  const _ChainNode({
    required this.title,
    required this.detail,
    this.isLast = false,
  });
}

class _DecisionChainRow extends StatelessWidget {
  final _ChainNode node;
  final bool isLast;

  const _DecisionChainRow({required this.node, required this.isLast});

  @override
  Widget build(BuildContext context) {
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          Column(
            children: [
              Container(
                width: 22,
                height: 22,
                decoration: BoxDecoration(
                  color: const Color(0xFF2563EB).withValues(alpha: 0.12),
                  shape: BoxShape.circle,
                  border: Border.all(color: const Color(0xFF2563EB)),
                ),
                alignment: Alignment.center,
                child: const Icon(
                  Icons.arrow_downward,
                  size: 12,
                  color: Color(0xFF2563EB),
                ),
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
              padding: EdgeInsets.only(bottom: isLast ? 0 : 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    node.title,
                    style: const TextStyle(
                      fontWeight: FontWeight.bold,
                      fontSize: 12,
                    ),
                  ),
                  const SizedBox(height: 1),
                  Text(
                    node.detail,
                    style: const TextStyle(fontSize: 11, color: Colors.grey),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class _FactorRow extends StatelessWidget {
  final String label;
  final String value;
  final bool isLast;

  const _FactorRow({
    required this.label,
    required this.value,
    this.isLast = false,
  });

  @override
  Widget build(BuildContext context) {
    final unavailable = value == ExplainableAssessmentWidget._unavailable;

    return Container(
      padding: const EdgeInsets.symmetric(vertical: 5),
      decoration: BoxDecoration(
        border: isLast
            ? null
            : Border(bottom: BorderSide(color: Colors.grey.withValues(alpha: 0.15))),
      ),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Expanded(
            child: Text(
              label,
              style: const TextStyle(fontSize: 11.5, color: Colors.black54),
            ),
          ),
          const SizedBox(width: 8),
          Text(
            value,
            textAlign: TextAlign.right,
            style: TextStyle(
              fontSize: 11.5,
              fontWeight: FontWeight.w600,
              fontStyle: unavailable ? FontStyle.italic : FontStyle.normal,
              color: unavailable ? Colors.grey : Colors.black87,
            ),
          ),
        ],
      ),
    );
  }
}
