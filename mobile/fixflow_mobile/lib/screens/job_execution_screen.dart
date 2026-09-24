import 'dart:async';
import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import '../providers/work_order_provider.dart';

class JobExecutionScreen extends StatefulWidget {
  final String workOrderId;

  const JobExecutionScreen({super.key, required this.workOrderId});

  @override
  State<JobExecutionScreen> createState() => _JobExecutionScreenState();
}

class _JobExecutionScreenState extends State<JobExecutionScreen> {
  final TextEditingController _signerController = TextEditingController();
  final TextEditingController _notesController = TextEditingController();
  final List<Offset?> _points = [];

  Timer? _timer;
  int _secondsElapsed = 0;
  bool _isSubmitting = false;

  final Map<String, bool> _checklist = {
    'Safety gear and area isolation verified': true,
    'Fault diagnosed and root cause addressed': true,
    'Component replacement / repair executed': true,
    'Operational test passed in presence of resident': false,
    'Work area cleaned and tools accounted for': false,
  };

  @override
  void initState() {
    super.initState();
    _startTimer();
  }

  void _startTimer() {
    _timer = Timer.periodic(const Duration(seconds: 1), (timer) {
      if (mounted) {
        setState(() {
          _secondsElapsed++;
        });
      }
    });
  }

  @override
  void dispose() {
    _timer?.cancel();
    _signerController.dispose();
    _notesController.dispose();
    super.dispose();
  }

  String _formatTimer(int totalSeconds) {
    final hours = totalSeconds ~/ 3600;
    final minutes = (totalSeconds % 3600) ~/ 60;
    final seconds = totalSeconds % 60;
    return '${hours.toString().padLeft(2, '0')}:${minutes.toString().padLeft(2, '0')}:${seconds.toString().padLeft(2, '0')}';
  }

  Future<void> _handleSubmit() async {
    final signer = _signerController.text.trim();
    if (signer.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please enter resident / customer name.')),
      );
      return;
    }

    if (_points.isEmpty) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Please capture customer signature on pad.')),
      );
      return;
    }

    setState(() => _isSubmitting = true);

    const mockSvgSig =
        "data:image/svg+xml;utf8,<svg xmlns='http://www.w3.org/2000/svg' width='160' height='40'><path d='M10 30 Q 50 10 90 30 T 150 20' stroke='#2563eb' fill='none' stroke-width='2'/></svg>";

    final success = await context.read<WorkOrderProvider>().completeJob(
          widget.workOrderId,
          signerName: signer,
          signatureDataUrl: mockSvgSig,
          notes: _notesController.text.trim(),
        );

    setState(() => _isSubmitting = false);

    if (success && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Work order marked as Completed with customer sign-off!')),
      );
      Navigator.pop(context);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Job Execution & Sign-off'),
      ),
      body: ListView(
        padding: const EdgeInsets.all(16),
        children: [
          // Live Execution Timer Card
          Card(
            color: Theme.of(context).primaryColor,
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 20, horizontal: 16),
              child: Column(
                children: [
                  const Text(
                    'ACTIVE EXECUTION TIMER',
                    style: TextStyle(
                      color: Colors.white70,
                      fontSize: 12,
                      fontWeight: FontWeight.bold,
                      letterSpacing: 1.1,
                    ),
                  ),
                  const SizedBox(height: 8),
                  Text(
                    _formatTimer(_secondsElapsed),
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 34,
                      fontWeight: FontWeight.bold,
                      fontFamily: 'monospace',
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Completion Checklist
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Work Order Checklist',
                    style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  ..._checklist.keys.map(
                    (key) => CheckboxListTile(
                      dense: true,
                      contentPadding: EdgeInsets.zero,
                      title: Text(key, style: const TextStyle(fontSize: 13)),
                      value: _checklist[key],
                      onChanged: (val) {
                        setState(() {
                          _checklist[key] = val ?? false;
                        });
                      },
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 16),

          // Completion Notes & Signer
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Customer Verification & Sign-off',
                    style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _signerController,
                    decoration: InputDecoration(
                      labelText: 'Customer / Resident Full Name',
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                      isDense: true,
                    ),
                  ),
                  const SizedBox(height: 12),
                  TextField(
                    controller: _notesController,
                    decoration: InputDecoration(
                      labelText: 'Completion Notes',
                      hintText: 'e.g. Verified AC cooling, noise eliminated',
                      border: OutlineInputBorder(borderRadius: BorderRadius.circular(8)),
                      isDense: true,
                    ),
                    maxLines: 2,
                  ),
                  const SizedBox(height: 16),

                  // Digital Signature Pad
                  Row(
                    mainAxisAlignment: MainAxisAlignment.spaceBetween,
                    children: [
                      const Text(
                        'Resident Digital Signature',
                        style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600),
                      ),
                      TextButton(
                        onPressed: () {
                          setState(() {
                            _points.clear();
                          });
                        },
                        child: const Text('Clear Pad', style: TextStyle(fontSize: 12)),
                      ),
                    ],
                  ),
                  const SizedBox(height: 6),
                  Container(
                    height: 140,
                    width: double.infinity,
                    decoration: BoxDecoration(
                      color: Colors.grey.shade50,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: Colors.grey.shade300),
                    ),
                    child: GestureDetector(
                      onPanUpdate: (details) {
                        final renderBox = context.findRenderObject() as RenderBox?;
                        if (renderBox != null) {
                          setState(() {
                            _points.add(details.localPosition);
                          });
                        }
                      },
                      onPanEnd: (details) => _points.add(null),
                      child: CustomPaint(
                        painter: _SignaturePainter(_points),
                        size: Size.infinite,
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 20),

          // Complete Button
          ElevatedButton.icon(
            icon: _isSubmitting
                ? const SizedBox(width: 18, height: 18, child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2))
                : const Icon(Icons.check_circle),
            label: Text(_isSubmitting ? 'Submitting Verification...' : 'Submit Customer Sign-off & Complete Job'),
            style: ElevatedButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 16),
              backgroundColor: Colors.green.shade600,
              foregroundColor: Colors.white,
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
            ),
            onPressed: _isSubmitting ? null : _handleSubmit,
          ),
          const SizedBox(height: 24),
        ],
      ),
    );
  }
}

class _SignaturePainter extends CustomPainter {
  final List<Offset?> points;

  _SignaturePainter(this.points);

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = const Color(0xFF2563EB)
      ..strokeCap = StrokeCap.round
      ..strokeWidth = 3.0;

    for (int i = 0; i < points.length - 1; i++) {
      if (points[i] != null && points[i + 1] != null) {
        canvas.drawLine(points[i]!, points[i + 1]!, paint);
      }
    }
  }

  @override
  bool shouldRepaint(covariant _SignaturePainter oldDelegate) => true;
}
