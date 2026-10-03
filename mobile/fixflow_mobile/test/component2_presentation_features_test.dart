import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fixflow_mobile/models/agent_workflow_model.dart';
import 'package:fixflow_mobile/models/priority_assessment_model.dart';
import 'package:fixflow_mobile/widgets/agent_timeline_widget.dart';
import 'package:fixflow_mobile/widgets/explainable_assessment_widget.dart';
import 'package:fixflow_mobile/widgets/priority_badge.dart';
import 'package:fixflow_mobile/widgets/risk_gauge_widget.dart';

// Palette asserted here is the one already used by priority_badge.dart and by
// the backend risk bands. It is repeated deliberately so a change to the
// displayed bands fails these tests instead of silently passing.
const Color _kLow = Color(0xFF10B981);
const Color _kMedium = Color(0xFF2563EB);
const Color _kHigh = Color(0xFFF59E0B);
const Color _kCritical = Color(0xFFDC2626);

PriorityAssessmentModel _assessment({
  ContributingFactors? contributingFactors,
  String status = 'Completed',
  int riskScore = 87,
  String riskLevel = 'Critical',
  String priority = 'Critical',
  String assetCriticality = 'Critical',
  String impactLevel = 'High',
  String likelihoodLevel = 'High',
  bool escalationFlag = false,
  String? escalationReason,
  String explanation = 'High impact asset with a recorded safety hazard.',
  String recommendedResponseWindow = 'Immediate (Within 1 hour)',
  int responseTimeHours = 1,
  int resolutionTimeHours = 4,
}) {
  return PriorityAssessmentModel(
    id: 'pa-1',
    requestId: 'req-1',
    requestNumber: 'REQ-0001',
    requestTitle: 'Chiller failure on line 3',
    assetCriticality: assetCriticality,
    impactLevel: impactLevel,
    likelihoodLevel: likelihoodLevel,
    riskScore: riskScore,
    riskLevel: riskLevel,
    priority: priority,
    recommendedResponseWindow: recommendedResponseWindow,
    responseTimeHours: responseTimeHours,
    resolutionTimeHours: resolutionTimeHours,
    escalationFlag: escalationFlag,
    escalationReason: escalationReason,
    explanation: explanation,
    status: status,
    contributingFactors: contributingFactors,
  );
}

Widget _host(Widget child) {
  return MaterialApp(
    home: Scaffold(body: SingleChildScrollView(child: child)),
  );
}

Color _bandBorderColor(WidgetTester tester, String band) {
  final container = tester.widget<Container>(
    find.byKey(ValueKey('risk_band_$band')),
  );
  final decoration = container.decoration! as BoxDecoration;
  return (decoration.border! as Border).top.color;
}

void main() {
  // ===========================================================================
  // Feature 2: Animated Risk Gauge
  // ===========================================================================
  group('RiskGaugeWidget', () {
    testWidgets('F1. renders the gauge header', (tester) async {
      await tester.pumpWidget(
        _host(const RiskGaugeWidget(riskScore: 87, riskLevel: 'Critical')),
      );

      expect(find.text('ANIMATED RISK GAUGE'), findsOneWidget);
    });

    testWidgets('F2. animates from 0 up to the validated score', (tester) async {
      await tester.pumpWidget(
        _host(const RiskGaugeWidget(riskScore: 87, riskLevel: 'Critical')),
      );

      // First frame of the reveal.
      expect(find.text('0'), findsOneWidget);
      expect(find.text('87'), findsNothing);

      await tester.pumpAndSettle();

      expect(find.text('87'), findsOneWidget);
      expect(find.text('0'), findsNothing);
    });

    testWidgets('F3. displays the score exactly as supplied, never a copy',
        (tester) async {
      for (final entry in <int, String>{
        1: '1',
        25: '25',
        42: '42',
        76: '76',
        100: '100',
      }.entries) {
        await tester.pumpWidget(
          _host(RiskGaugeWidget(riskScore: entry.key, riskLevel: 'High')),
        );
        await tester.pumpAndSettle();

        expect(find.text(entry.value), findsOneWidget,
            reason: 'score ${entry.key} must be rendered verbatim');
      }
    });

    testWidgets('F4. shows the backend risk level badge verbatim',
        (tester) async {
      await tester.pumpWidget(
        _host(const RiskGaugeWidget(riskScore: 87, riskLevel: 'Critical')),
      );
      await tester.pumpAndSettle();

      expect(find.text('RISK: CRITICAL'), findsOneWidget);
    });

    testWidgets('F5. draws the four fixed backend bands as a scale',
        (tester) async {
      await tester.pumpWidget(
        _host(const RiskGaugeWidget(riskScore: 87, riskLevel: 'Critical')),
      );
      await tester.pumpAndSettle();

      expect(find.text('1-25 LOW'), findsOneWidget);
      expect(find.text('26-50 MEDIUM'), findsOneWidget);
      expect(find.text('51-75 HIGH'), findsOneWidget);
      expect(find.text('76-100 CRITICAL'), findsOneWidget);
    });

    testWidgets('F6. highlights the band named by the authoritative risk level',
        (tester) async {
      await tester.pumpWidget(
        _host(const RiskGaugeWidget(riskScore: 63, riskLevel: 'High')),
      );
      await tester.pumpAndSettle();

      expect(_bandBorderColor(tester, 'high'), equals(_kHigh));
      expect(_bandBorderColor(tester, 'low'), isNot(equals(_kLow)));
      expect(_bandBorderColor(tester, 'medium'), isNot(equals(_kMedium)));
      expect(_bandBorderColor(tester, 'critical'), isNot(equals(_kCritical)));
    });

    testWidgets(
        'F7. never re-derives the level from the score: a contradictory '
        'level is displayed as the backend reported it', (tester) async {
      // 92 sits in the Critical band, but the widget must show whatever level
      // the validated assessment carries instead of computing one itself.
      await tester.pumpWidget(
        _host(const RiskGaugeWidget(riskScore: 92, riskLevel: 'Low')),
      );
      await tester.pumpAndSettle();

      expect(find.text('RISK: LOW'), findsOneWidget);
      expect(_bandBorderColor(tester, 'low'), equals(_kLow));
      expect(_bandBorderColor(tester, 'critical'), isNot(equals(_kCritical)));
    });

    testWidgets('F8. safe failure shows an unavailable state with no number',
        (tester) async {
      await tester.pumpWidget(_host(const RiskGaugeWidget(riskScore: null)));
      await tester.pumpAndSettle();

      expect(find.text('RISK SCORE UNAVAILABLE'), findsOneWidget);
      expect(find.text('/ 100'), findsNothing);
      expect(find.text('0'), findsNothing);
      expect(find.byKey(const ValueKey('risk_band_low')), findsNothing);
      expect(find.byKey(const ValueKey('risk_band_critical')), findsNothing);
      expect(find.byType(RiskLevelBadgeWidget), findsNothing);
    });

    testWidgets('F9. states that the score is not recalculated by the app',
        (tester) async {
      await tester.pumpWidget(
        _host(const RiskGaugeWidget(riskScore: 87, riskLevel: 'Critical')),
      );
      await tester.pumpAndSettle();

      expect(
        find.textContaining('The app does not recalculate it.'),
        findsOneWidget,
      );
    });
  });

  // ===========================================================================
  // Feature 3: Explainable Risk & Priority Assessment
  // ===========================================================================
  group('ExplainableAssessmentWidget', () {
    testWidgets('E1. renders the explainability header', (tester) async {
      await tester.pumpWidget(
        _host(ExplainableAssessmentWidget(assessment: _assessment())),
      );

      expect(
        find.text('EXPLAINABLE RISK & PRIORITY ASSESSMENT'),
        findsOneWidget,
      );
    });

    testWidgets('E2. shows the real contributing factors reported by the backend',
        (tester) async {
      const factors = ContributingFactors(
        assetCriticality: 'Critical',
        baseMatrixScore: 48,
        assetCriticalityScore: 28,
        impactScore: 3,
        likelihoodScore: 4,
        hasSafetyHazard: true,
        safetyHazardModifier: 25,
        recentFailureCount: 3,
        recurrenceModifier: 9,
        locationModifier: 5,
        operationalDisruption: 'Severe',
      );

      await tester.pumpWidget(
        _host(
          ExplainableAssessmentWidget(
            assessment: _assessment(contributingFactors: factors),
          ),
        ),
      );

      expect(find.text('48'), findsOneWidget);
      expect(find.text('28'), findsOneWidget);
      expect(find.text('25'), findsOneWidget);
      expect(find.text('9'), findsOneWidget);
      expect(find.text('5'), findsOneWidget);
      expect(find.text('3'), findsWidgets);
      expect(find.text('Severe'), findsOneWidget);
      expect(find.text('Yes'), findsOneWidget);
      expect(find.text('Unavailable'), findsNothing);
    });

    testWidgets('E3. reports absent factors as unavailable instead of guessing',
        (tester) async {
      // Only two of the eleven factor fields were reported.
      const factors = ContributingFactors(
        baseMatrixScore: 16,
        hasSafetyHazard: false,
      );

      await tester.pumpWidget(
        _host(
          ExplainableAssessmentWidget(
            assessment: _assessment(
              contributingFactors: factors,
              riskScore: 30,
              riskLevel: 'Medium',
              priority: 'Medium',
            ),
          ),
        ),
      );

      expect(find.text('16'), findsOneWidget);
      expect(find.text('No'), findsOneWidget);
      // assetCriticalityScore, impactScore, likelihoodScore,
      // safetyHazardModifier, recentFailureCount, recurrenceModifier,
      // locationModifier, operationalDisruption were never reported.
      expect(find.text('Unavailable'), findsNWidgets(8));
    });

    testWidgets('E4. marks every factor unavailable when the backend sent none',
        (tester) async {
      await tester.pumpWidget(
        _host(
          ExplainableAssessmentWidget(
            assessment: _assessment(
              contributingFactors: null,
              riskScore: 55,
              riskLevel: 'High',
              priority: 'High',
            ),
          ),
        ),
      );

      expect(find.text('Unavailable'), findsWidgets);
      expect(find.text('Base Matrix Score'), findsOneWidget);
    });

    testWidgets('E5. keeps Risk Score, Risk Level and Priority as distinct steps',
        (tester) async {
      await tester.pumpWidget(
        _host(ExplainableAssessmentWidget(assessment: _assessment())),
      );

      expect(find.text('Risk Score'), findsOneWidget);
      expect(find.text('Risk Level'), findsOneWidget);
      expect(find.text('Priority'), findsOneWidget);
      expect(find.text('87 / 100'), findsOneWidget);
    });

    testWidgets('E6. states that a risk score is not a priority',
        (tester) async {
      await tester.pumpWidget(
        _host(ExplainableAssessmentWidget(assessment: _assessment())),
      );

      expect(
        find.textContaining('A risk score is not a priority.'),
        findsOneWidget,
      );
    });

    testWidgets('E7. shows the full decision chain in order', (tester) async {
      await tester.pumpWidget(
        _host(ExplainableAssessmentWidget(assessment: _assessment())),
      );

      final chain = <String>[
        'Assessment Factors',
        'Validated Risk Assessment',
        'Risk Score',
        'Risk Level',
        'Priority',
        'Existing SLA / Response',
        'Escalation',
      ];

      for (final node in chain) {
        expect(find.text(node), findsOneWidget, reason: 'missing node $node');
      }

      final offsets = chain
          .map((node) => tester.getTopLeft(find.text(node)).dy)
          .toList();
      for (var i = 1; i < offsets.length; i++) {
        expect(offsets[i], greaterThan(offsets[i - 1]),
            reason: '${chain[i]} must follow ${chain[i - 1]}');
      }
    });

    testWidgets('E8. repeats the SLA values from the assessment, never invented',
        (tester) async {
      await tester.pumpWidget(
        _host(
          ExplainableAssessmentWidget(
            assessment: _assessment(
              recommendedResponseWindow: 'Within 2 hours',
              responseTimeHours: 2,
              resolutionTimeHours: 8,
              riskLevel: 'High',
              priority: 'High',
              riskScore: 60,
            ),
          ),
        ),
      );

      expect(
        find.textContaining('Within 2 hours | 2h dispatch | 8h resolution'),
        findsOneWidget,
      );
    });

    testWidgets('E9. safe failure reports unavailable instead of a score',
        (tester) async {
      await tester.pumpWidget(
        _host(
          ExplainableAssessmentWidget(
            assessment: _assessment(
              status: 'Failed',
              riskScore: 0,
              riskLevel: '',
              priority: '',
              explanation: '',
            ),
          ),
        ),
      );

      expect(
        find.textContaining('This assessment failed safely'),
        findsOneWidget,
      );
      expect(find.text('0 / 100'), findsNothing);
      expect(find.text('Unavailable'), findsWidgets);
    });

    testWidgets('E10. shows the escalation reason recorded by the backend',
        (tester) async {
      await tester.pumpWidget(
        _host(
          ExplainableAssessmentWidget(
            assessment: _assessment(
              escalationFlag: true,
              escalationReason: 'Safety hazard on a critical asset',
            ),
          ),
        ),
      );

      expect(
        find.textContaining('Escalated: Safety hazard on a critical asset'),
        findsOneWidget,
      );
    });

    testWidgets('E11. shows the agent explanation as the decision audit',
        (tester) async {
      await tester.pumpWidget(
        _host(ExplainableAssessmentWidget(assessment: _assessment())),
      );

      expect(
        find.text('High impact asset with a recorded safety hazard.'),
        findsOneWidget,
      );
      expect(
        find.text('Agent Explanation / Decision Audit'),
        findsOneWidget,
      );
    });
  });

  // ===========================================================================
  // Feature 1: Agent Processing Timeline
  // ===========================================================================
  group('AgentWorkflowModel parsing', () {
    test('T1. parses the recorded workflow, steps and tool calls', () {
      final workflow = AgentWorkflowModel.fromJson(<String, dynamic>{
        'id': 'wf-1',
        'requestId': 'req-1',
        'requestTitle': 'Chiller failure on line 3',
        'workflowType': 'Priority',
        'status': 'Completed',
        'startedAt': '2026-10-02T09:15:00Z',
        'completedAt': '2026-10-02T09:15:01Z',
        'steps': <Map<String, dynamic>>[
          <String, dynamic>{
            'id': 'step-1',
            'agentName': 'PriorityAgent',
            'stepName': 'Priority Assessment',
            'status': 'Completed',
            'startedAt': '2026-10-02T09:15:00Z',
            'completedAt': '2026-10-02T09:15:00Z',
            'toolCalls': <Map<String, dynamic>>[
              <String, dynamic>{
                'id': 'tc-1',
                'toolName': 'GetAssetCriticalityTool',
                'executionTimeMs': 42,
                'success': true,
              },
              <String, dynamic>{
                'id': 'tc-2',
                'toolName': 'GetSLAConfigTool',
                'executionTimeMs': 7,
                'success': true,
              },
            ],
          },
        ],
      });

      expect(workflow.workflowType, equals('Priority'));
      expect(workflow.status, equals('Completed'));
      expect(workflow.steps, hasLength(1));
      expect(workflow.steps.single.agentName, equals('PriorityAgent'));
      expect(workflow.steps.single.toolCalls, hasLength(2));
      expect(
        workflow.steps.single.toolCalls.first.executionTimeMs,
        equals(42),
      );
      expect(workflow.recordedAt, isNotNull);
      expect(
        workflow.recordedAtText,
        matches(RegExp(r'^\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}$')),
      );
    });

    test('T2. tolerates a payload with no steps or timestamps', () {
      final workflow = AgentWorkflowModel.fromJson(<String, dynamic>{
        'id': 'wf-2',
        'requestId': 'req-2',
        'workflowType': 'Priority',
        'status': 'Failed',
      });

      expect(workflow.steps, isEmpty);
      expect(workflow.recordedAt, isNull);
      expect(workflow.recordedAtText, isNull);
    });

    test('T3. does not derive a step duration from identical timestamps', () {
      final step = AgentStepModel.fromJson(<String, dynamic>{
        'id': 'step-1',
        'agentName': 'PriorityAgent',
        'status': 'Completed',
        'startedAt': '2026-10-02T09:15:00Z',
        'completedAt': '2026-10-02T09:15:00Z',
      });

      // The backend writes both instants as the same value, so only a single
      // "recorded" instant is exposed. No elapsed-time getter exists.
      expect(step.recordedAt, equals(step.startedAt));
    });
  });

  group('WorkflowStatusStyle', () {
    test('T4. maps every backend WorkflowStatus string', () {
      expect(WorkflowStatusStyle.of('Completed').label, equals('COMPLETED'));
      expect(WorkflowStatusStyle.of('Failed').label, equals('FAILED'));
      expect(
        WorkflowStatusStyle.of('WaitingForApproval').label,
        equals('WAITING FOR APPROVAL'),
      );
      expect(WorkflowStatusStyle.of('Approved').label, equals('APPROVED'));
      expect(WorkflowStatusStyle.of('Rejected').label, equals('REJECTED'));
      expect(WorkflowStatusStyle.of('Running').label, equals('RUNNING'));
      expect(WorkflowStatusStyle.of('Pending').label, equals('PENDING'));
    });

    test('T5. flags only Running as executing', () {
      expect(WorkflowStatusStyle.of('Running').isRunning, isTrue);
      for (final status in <String>[
        'Completed',
        'Failed',
        'WaitingForApproval',
        'Approved',
        'Rejected',
        'Pending',
      ]) {
        expect(WorkflowStatusStyle.of(status).isRunning, isFalse,
            reason: '$status must not be animated as executing');
      }
    });

    test('T6. keeps the raw text of an unknown status', () {
      expect(
        WorkflowStatusStyle.of('PausedForReview').label,
        equals('PAUSED FOR REVIEW'),
      );
      expect(WorkflowStatusStyle.of('').label, equals('UNKNOWN'));
    });
  });

  group('AgentWorkflowTimelineView', () {
    AgentWorkflowModel workflowWith(List<AgentStepModel> steps,
        {String status = 'Completed',
        String type = 'Priority',
        bool withWorkflowTimestamp = true}) {
      // Local instants so the rendered text is timezone independent. The
      // backend writes startedAt and completedAt as the same value, mirrored
      // here.
      final recorded = DateTime(2026, 10, 2, 9, 15);
      return AgentWorkflowModel(
        id: 'wf-1',
        requestId: 'req-1',
        requestTitle: 'Chiller failure on line 3',
        workflowType: type,
        status: status,
        startedAt: withWorkflowTimestamp ? recorded : null,
        completedAt: withWorkflowTimestamp ? recorded : null,
        steps: steps,
      );
    }

    testWidgets('T7. renders the recorded agents, steps and statuses',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(const <AgentStepModel>[
              AgentStepModel(
                id: 'step-1',
                agentName: 'PriorityAgent',
                stepName: 'Priority Assessment',
                status: 'Completed',
              ),
            ]),
          ),
        ),
      );

      expect(find.text('Workflow: Priority'), findsOneWidget);
      expect(find.text('Status: COMPLETED'), findsOneWidget);
      expect(find.text('PriorityAgent'), findsOneWidget);
      expect(find.text('Priority Assessment'), findsOneWidget);
      expect(find.text('COMPLETED'), findsWidgets);
    });

    testWidgets('T8. invents no stages beyond the recorded steps',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(const <AgentStepModel>[
              AgentStepModel(
                id: 'step-1',
                agentName: 'PriorityAgent',
                stepName: 'Priority Assessment',
                status: 'Completed',
              ),
            ]),
          ),
        ),
      );

      expect(find.text('PriorityAgent'), findsOneWidget);
      expect(find.text('ClassificationAgent'), findsNothing);
      expect(find.text('AssignmentAgent'), findsNothing);
      expect(find.text('SchedulingAgent'), findsNothing);
    });

    testWidgets('T9. renders every recorded step of a full pipeline',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(
              const <AgentStepModel>[
                AgentStepModel(
                    id: 's1',
                    agentName: 'ClassificationAgent',
                    stepName: 'Request Classification',
                    status: 'Completed'),
                AgentStepModel(
                    id: 's2',
                    agentName: 'PriorityAgent',
                    stepName: 'Priority Assessment',
                    status: 'Completed'),
                AgentStepModel(
                    id: 's3',
                    agentName: 'AssignmentAgent',
                    stepName: 'Technician Matching',
                    status: 'Completed'),
                AgentStepModel(
                    id: 's4',
                    agentName: 'SchedulingAgent',
                    stepName: 'Work Order Scheduling',
                    status: 'Completed'),
              ],
              type: 'FullPipeline',
            ),
          ),
        ),
      );

      expect(find.text('Workflow: FullPipeline'), findsOneWidget);
      expect(find.text('ClassificationAgent'), findsOneWidget);
      expect(find.text('PriorityAgent'), findsOneWidget);
      expect(find.text('AssignmentAgent'), findsOneWidget);
      expect(find.text('SchedulingAgent'), findsOneWidget);
    });

    testWidgets('T10. shows no spinner for an already finished workflow',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(const <AgentStepModel>[
              AgentStepModel(
                id: 'step-1',
                agentName: 'PriorityAgent',
                stepName: 'Priority Assessment',
                status: 'Completed',
              ),
            ]),
          ),
        ),
      );

      expect(find.byType(CircularProgressIndicator), findsNothing);
      expect(
        find.textContaining('This is not a live execution stream'),
        findsOneWidget,
      );
    });

    testWidgets('T11. animates only the step the backend recorded as Running',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(
              const <AgentStepModel>[
                AgentStepModel(
                  id: 'step-1',
                  agentName: 'ClassificationAgent',
                  stepName: 'Request Classification',
                  status: 'Completed',
                ),
                AgentStepModel(
                  id: 'step-2',
                  agentName: 'PriorityAgent',
                  stepName: 'Priority Assessment',
                  status: 'Running',
                ),
              ],
              status: 'Running',
            ),
          ),
        ),
      );

      expect(find.byType(CircularProgressIndicator), findsOneWidget);
      expect(find.text('RUNNING'), findsWidgets);
    });

    testWidgets('T12. shows a failed step and its failed tool call',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(
              const <AgentStepModel>[
                AgentStepModel(
                  id: 'step-1',
                  agentName: 'PriorityAgent',
                  stepName: 'Priority Assessment',
                  status: 'Failed',
                  toolCalls: <AgentToolCallModel>[
                    AgentToolCallModel(
                      toolName: 'GetRecentFailuresTool',
                      success: false,
                      executionTimeMs: 5003,
                    ),
                  ],
                ),
              ],
              status: 'Failed',
            ),
          ),
        ),
      );

      expect(find.text('Status: FAILED'), findsOneWidget);
      expect(find.text('GetRecentFailuresTool'), findsOneWidget);
      expect(find.text('5003 ms'), findsOneWidget);
      expect(find.byIcon(Icons.error_outline), findsOneWidget);
    });

    testWidgets('T13. shows the real per-tool execution timings',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(const <AgentStepModel>[
              AgentStepModel(
                id: 'step-1',
                agentName: 'PriorityAgent',
                stepName: 'Priority Assessment',
                status: 'Completed',
                toolCalls: <AgentToolCallModel>[
                  AgentToolCallModel(
                    toolName: 'GetAssetCriticalityTool',
                    success: true,
                    executionTimeMs: 42,
                  ),
                  AgentToolCallModel(
                    toolName: 'GetSLAConfigTool',
                    success: true,
                    executionTimeMs: 7,
                  ),
                ],
              ),
            ]),
          ),
        ),
      );

      expect(find.text('GetAssetCriticalityTool'), findsOneWidget);
      expect(find.text('42 ms'), findsOneWidget);
      expect(find.text('GetSLAConfigTool'), findsOneWidget);
      expect(find.text('7 ms'), findsOneWidget);
      expect(find.byIcon(Icons.check_circle_outline), findsNWidgets(2));
    });

    testWidgets('T14. reports a step with no recorded tool calls',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(const <AgentStepModel>[
              AgentStepModel(
                id: 'step-1',
                agentName: 'PriorityAgent',
                stepName: 'Priority Assessment',
                status: 'Completed',
              ),
            ]),
          ),
        ),
      );

      expect(find.text('No tool calls recorded for this step.'), findsOneWidget);
    });

    testWidgets('T15. reports a workflow persisted without steps',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(const <AgentStepModel>[]),
          ),
        ),
      );

      expect(
        find.text('The backend recorded this workflow without any agent steps.'),
        findsOneWidget,
      );
    });

    testWidgets('T16. surfaces a waiting-for-approval workflow accurately',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(
              const <AgentStepModel>[
                AgentStepModel(
                  id: 'step-1',
                  agentName: 'PriorityAgent',
                  stepName: 'Priority Assessment',
                  status: 'WaitingForApproval',
                ),
              ],
              status: 'WaitingForApproval',
            ),
          ),
        ),
      );

      expect(find.text('Status: WAITING FOR APPROVAL'), findsOneWidget);
      expect(find.byType(CircularProgressIndicator), findsNothing);
    });

    testWidgets('T17. omits a recorded instant the backend never stored',
        (tester) async {
      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(
              const <AgentStepModel>[
                AgentStepModel(
                  id: 'step-1',
                  agentName: 'PriorityAgent',
                  stepName: 'Priority Assessment',
                  status: 'Completed',
                ),
              ],
              withWorkflowTimestamp: false,
            ),
          ),
        ),
      );

      // Neither the workflow banner nor the step row may invent a timestamp.
      expect(find.textContaining('Recorded:'), findsNothing);
    });

    testWidgets('T18. shows the recorded timestamps the backend did store',
        (tester) async {
      final recorded = DateTime(2026, 10, 2, 9, 15);

      await tester.pumpWidget(
        _host(
          AgentWorkflowTimelineView(
            workflow: workflowWith(<AgentStepModel>[
              AgentStepModel(
                id: 'step-1',
                agentName: 'PriorityAgent',
                stepName: 'Priority Assessment',
                status: 'Completed',
                completedAt: recorded,
              ),
            ]),
          ),
        ),
      );

      const expected = 'Recorded: 2026-10-02 09:15:00';
      // One from the workflow banner, one from the step row.
      expect(find.text(expected), findsNWidgets(2));
    });
  });
}
