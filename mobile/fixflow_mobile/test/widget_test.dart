import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';
import 'package:provider/provider.dart';

import 'package:fixflow_mobile/main.dart';
import 'package:fixflow_mobile/models/request_model.dart';
import 'package:fixflow_mobile/providers/request_provider.dart';
import 'package:fixflow_mobile/screens/requests/submit_request_screen.dart';
import 'package:fixflow_mobile/providers/auth_provider.dart';

void main() {
  // ── Existing shared foundation test ───────────────────────────────────────
  testWidgets('FixFlow app starts successfully', (WidgetTester tester) async {
    await tester.pumpWidget(const FixFlowApp());
    expect(find.byType(MaterialApp), findsOneWidget);
  });

  // =========================================================================
  // MEMBER 1 — Request Intake & Classification widget / unit tests
  // =========================================================================

  // ── 1. RequestProvider default state ──────────────────────────────────────
  group('RequestProvider initial state', () {
    test('isLoading is false by default', () {
      final provider = RequestProvider();
      expect(provider.isLoading, isFalse);
    });

    test('error is null by default', () {
      final provider = RequestProvider();
      expect(provider.error, isNull);
    });

    test('myRequests list is empty by default', () {
      final provider = RequestProvider();
      expect(provider.myRequests, isEmpty);
    });

    test('categories list is empty by default', () {
      final provider = RequestProvider();
      expect(provider.categories, isEmpty);
    });

    test('locations list is empty by default', () {
      final provider = RequestProvider();
      expect(provider.locations, isEmpty);
    });
  });

  // ── 2. RequestSummary.fromJson ─────────────────────────────────────────────
  group('RequestSummary.fromJson', () {
    final json = {
      'id': 'abc-123',
      'requestNumber': 'REQ-2026-0001',
      'title': 'Leaking tap',
      'status': 'Submitted',
      'category': 'Plumbing',
      'requesterId': 'user-1',
      'requesterName': 'Alice Smith',
      'locationName': 'Tower A - Floor 3',
      'createdAt': '2026-09-25T08:00:00Z',
      'hasClassification': false,
    };

    test('parses all fields correctly', () {
      final model = RequestSummary.fromJson(json);
      expect(model.id,                'abc-123');
      expect(model.requestNumber,     'REQ-2026-0001');
      expect(model.title,             'Leaking tap');
      expect(model.status,            'Submitted');
      expect(model.category,          'Plumbing');
      expect(model.requesterName,     'Alice Smith');
      expect(model.locationName,      'Tower A - Floor 3');
      expect(model.hasClassification, isFalse);
    });

    test('parses null optional fields without error', () {
      final sparse = {
        'id': 'x', 'requestNumber': 'REQ-2026-0002',
        'title': 'T', 'status': 'Submitted',
        'requesterId': 'u', 'requesterName': 'Bob',
        'createdAt': '2026-09-25T08:00:00Z', 'hasClassification': false,
      };
      final model = RequestSummary.fromJson(sparse);
      expect(model.category,     isNull);
      expect(model.locationName, isNull);
    });
  });

  // ── 3. ClassificationResult.fromJson ──────────────────────────────────────
  group('ClassificationResult.fromJson', () {
    final json = {
      'id': 'cl-1',
      'category': 'Electrical',
      'subcategory': 'Power Outage',
      'confidenceScore': 0.85,
      'requiresReview': false,
      'requiredSkill': 'Residential Electrical Systems',
      'reason': 'High keyword match.',
      'isOverride': false,
      'overriddenByUserName': null,
      'createdAt': '2026-09-25T09:00:00Z',
    };

    test('parses confidence as double', () {
      final model = ClassificationResult.fromJson(json);
      expect(model.confidenceScore, closeTo(0.85, 0.001));
    });

    test('parses isOverride false', () {
      final model = ClassificationResult.fromJson(json);
      expect(model.isOverride, isFalse);
    });

    test('override row has overriddenByUserName set', () {
      final overrideJson = Map<String, dynamic>.from(json)
        ..['isOverride'] = true
        ..['overriddenByUserName'] = 'Manager Jane';
      final model = ClassificationResult.fromJson(overrideJson);
      expect(model.isOverride, isTrue);
      expect(model.overriddenByUserName, 'Manager Jane');
    });
  });

  // ── 4. SubmitRequestScreen form validation ─────────────────────────────────
  group('SubmitRequestScreen form validation', () {
    Widget buildTestable() {
      return MultiProvider(
        providers: [
          ChangeNotifierProvider<RequestProvider>(
              create: (_) => RequestProvider()),
          ChangeNotifierProvider<AuthProvider>(
              create: (_) => AuthProvider()),
        ],
        child: const MaterialApp(home: SubmitRequestScreen()),
      );
    }

    testWidgets('renders title and description fields', (tester) async {
      await tester.pumpWidget(buildTestable());
      await tester.pump(); // settle initState
      expect(find.text('Title *'), findsOneWidget);
      expect(find.text('Description *'), findsOneWidget);
    });

    testWidgets('shows validation error when submitted empty',
        (tester) async {
      await tester.pumpWidget(buildTestable());
      await tester.pump();

      // Tap Submit without filling anything in
      await tester.tap(find.text('Submit Request'));
      await tester.pump();

      expect(find.text('Title is required.'), findsOneWidget);
      expect(find.text('Description is required.'), findsOneWidget);
    });

    testWidgets('shows short-description validation error',
        (tester) async {
      await tester.pumpWidget(buildTestable());
      await tester.pump();

      await tester.enterText(
          find.widgetWithText(TextFormField, 'Title *'), 'Leaking tap');
      await tester.enterText(
          find.widgetWithText(TextFormField, 'Description *'), 'Short');
      await tester.tap(find.text('Submit Request'));
      await tester.pump();

      expect(find.text('At least 20 characters required.'), findsOneWidget);
    });
  });
}