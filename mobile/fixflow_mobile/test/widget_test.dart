import 'package:flutter/material.dart';
import 'package:flutter_test/flutter_test.dart';

import 'package:fixflow_mobile/main.dart';

void main() {
  testWidgets('FixFlow app starts successfully', (WidgetTester tester) async {
    await tester.pumpWidget(const FixFlowApp());

    expect(find.byType(MaterialApp), findsOneWidget);
  });
}