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
}
