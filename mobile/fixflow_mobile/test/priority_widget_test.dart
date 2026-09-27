import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:fixflow_mobile/models/priority_assessment_model.dart';
import 'package:fixflow_mobile/widgets/priority_badge.dart';

void main() {
  group('Priority & Risk Assessment Mobile Tests', () {
    test('PriorityAssessmentModel json parsing should parse all fields', () {
      final json = {
        'id': 'test-id-1',
        'requestId': 'req-id-1',
        'requestNumber': 'REQ-2026-0001',
        'requestTitle': 'Elevator door stuck',
        'assetName': 'Passenger Elevator',
        'assetCriticality': 'Critical',
        'impactLevel': 'High',
        'likelihoodLevel': 'Medium',
        'riskScore': 85,
        'riskLevel': 'Critical',
        'priority': 'Critical',
        'recommendedResponseWindow': 'Immediate (Within 1 hour)',
        'responseTimeHours': 1,
        'resolutionTimeHours': 4,
        'escalationFlag': true,
        'escalationReason': 'Safety hazard',
        'explanation': 'Elevator issue requires immediate attention',
        'status': 'Escalated',
      };

      final model = PriorityAssessmentModel.fromJson(json);

      expect(model.id, 'test-id-1');
      expect(model.requestNumber, 'REQ-2026-0001');
      expect(model.riskScore, 85);
      expect(model.riskLevel, 'Critical');
      expect(model.priority, 'Critical');
      expect(model.escalationFlag, isTrue);
      expect(model.responseTimeHours, 1);
    });

    testWidgets('PriorityBadgeWidget renders correct text and colors', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: PriorityBadgeWidget(priority: 'Critical'),
          ),
        ),
      );

      expect(find.text('CRITICAL'), findsOneWidget);
    });

    testWidgets('RiskLevelBadgeWidget renders risk prefix and text', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: RiskLevelBadgeWidget(riskLevel: 'High'),
          ),
        ),
      );

      expect(find.text('RISK: HIGH'), findsOneWidget);
    });

    testWidgets('SLAResponseCardWidget renders response window and target hours', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: SLAResponseCardWidget(
              window: 'Within 2 hours',
              responseHours: 2,
              resolutionHours: 8,
            ),
          ),
        ),
      );

      expect(find.text('SLA Response: Within 2 hours'), findsOneWidget);
      expect(find.text('Target: 2h dispatch • 8h resolution'), findsOneWidget);
    });
  });

  // ---------------------------------------------------------------------------
  // New tests: PriorityAssessmentModel — humanApprovalRequired & hazardDetected
  // ---------------------------------------------------------------------------
  group('PriorityAssessmentModel — new fields', () {
    test('fromJson parses humanApprovalRequired true and hazardDetected true', () {
      final json = {
        'id': 'id-2',
        'requestId': 'req-2',
        'requestNumber': 'REQ-2026-0002',
        'requestTitle': 'Boiler overheating',
        'assetCriticality': 'Critical',
        'impactLevel': 'High',
        'likelihoodLevel': 'High',
        'riskScore': 90,
        'riskLevel': 'Critical',
        'priority': 'Critical',
        'recommendedResponseWindow': 'Immediate (Within 1 hour)',
        'responseTimeHours': 1,
        'resolutionTimeHours': 4,
        'escalationFlag': true,
        'escalationReason': 'Safety risk',
        'explanation': 'Boiler hazard detected',
        'status': 'Escalated',
        'humanApprovalRequired': true,
        'hazardDetected': true,
      };

      final model = PriorityAssessmentModel.fromJson(json);

      expect(model.humanApprovalRequired, isTrue,
          reason: 'humanApprovalRequired must be parsed from JSON');
      expect(model.hazardDetected, isTrue,
          reason: 'hazardDetected must be parsed from JSON');
    });

    test('fromJson defaults humanApprovalRequired and hazardDetected to false when absent', () {
      final json = {
        'id': 'id-3',
        'requestId': 'req-3',
        'requestNumber': 'REQ-2026-0003',
        'requestTitle': 'Routine inspection',
        'assetCriticality': 'Low',
        'impactLevel': 'Low',
        'likelihoodLevel': 'Low',
        'riskScore': 10,
        'riskLevel': 'Low',
        'priority': 'Low',
        'recommendedResponseWindow': 'Within 5 business days',
        'responseTimeHours': 120,
        'resolutionTimeHours': 240,
        'escalationFlag': false,
        'explanation': 'No hazard',
        'status': 'Active',
        // humanApprovalRequired and hazardDetected deliberately omitted
      };

      final model = PriorityAssessmentModel.fromJson(json);

      expect(model.humanApprovalRequired, isFalse,
          reason: 'Should default to false when not in JSON');
      expect(model.hazardDetected, isFalse,
          reason: 'Should default to false when not in JSON');
    });

    test('fromJson parses FAILED status with humanApprovalRequired true', () {
      final json = {
        'id': 'id-4',
        'requestId': 'req-4',
        'requestNumber': 'REQ-2026-0004',
        'requestTitle': 'Agent error case',
        'assetCriticality': 'Medium',
        'impactLevel': 'Medium',
        'likelihoodLevel': 'Medium',
        'riskScore': 0,
        'riskLevel': 'Medium',
        'priority': 'Medium',
        'recommendedResponseWindow': 'Within 4 hours',
        'responseTimeHours': 4,
        'resolutionTimeHours': 24,
        'escalationFlag': false,
        'explanation': 'Agent execution failed',
        'status': 'FAILED',
        'humanApprovalRequired': true,
        'hazardDetected': false,
      };

      final model = PriorityAssessmentModel.fromJson(json);

      expect(model.status, equals('FAILED'),
          reason: 'FAILED status must be preserved as-is');
      expect(model.humanApprovalRequired, isTrue,
          reason: 'Tool failure sets humanApprovalRequired = true');
      expect(model.hazardDetected, isFalse);
    });
  });

  // ---------------------------------------------------------------------------
  // Additional badge tests for extra coverage
  // ---------------------------------------------------------------------------
  group('Badge widgets — additional levels', () {
    testWidgets('PriorityBadgeWidget renders Low priority', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: PriorityBadgeWidget(priority: 'Low'),
          ),
        ),
      );

      expect(find.text('LOW'), findsOneWidget);
    });

    testWidgets('RiskLevelBadgeWidget renders Critical risk level', (WidgetTester tester) async {
      await tester.pumpWidget(
        const MaterialApp(
          home: Scaffold(
            body: RiskLevelBadgeWidget(riskLevel: 'Critical'),
          ),
        ),
      );

      expect(find.text('RISK: CRITICAL'), findsOneWidget);
    });
  });
}
