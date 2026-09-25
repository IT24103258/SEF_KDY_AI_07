import 'package:flutter/material.dart';
import 'package:provider/provider.dart';
import 'package:intl/intl.dart';
import '../core/theme/app_colors.dart';
import '../providers/work_order_provider.dart';

class DigitalSignatureScreen extends StatefulWidget {
  final String workOrderId;
  final int elapsedSeconds;
  final int photoCount;
  final int noteCount;
  final String? uploadedFileKey;

  const DigitalSignatureScreen({
    super.key,
    required this.workOrderId,
    this.elapsedSeconds = 0,
    this.photoCount = 0,
    this.noteCount = 0,
    this.uploadedFileKey,
  });

  @override
  State<DigitalSignatureScreen> createState() => _DigitalSignatureScreenState();
}

class _DigitalSignatureScreenState extends State<DigitalSignatureScreen> {
  final _formKey = GlobalKey<FormState>();
  final _customerNameController = TextEditingController();
  final List<Offset?> _signaturePoints = [];
  bool _signatureCaptured = false;
  bool _isSubmitting = false;
  bool _hasSubmitted = false;

  @override
  void dispose() {
    _customerNameController.dispose();
    super.dispose();
  }

  String _formatDuration(int totalSeconds) {
    final hours = totalSeconds ~/ 3600;
    final minutes = (totalSeconds % 3600) ~/ 60;
    final seconds = totalSeconds % 60;
    if (hours > 0) {
      return '${hours}h ${minutes}m ${seconds}s';
    }
    return '${minutes}m ${seconds}s';
  }

  String _buildSignatureDataUrl() {
    // Encode the captured points as a simple SVG data URL
    // This is the actual captured path — not a placeholder
    if (_signaturePoints.isEmpty) return '';

    final buffer = StringBuffer();
    buffer.write("<svg xmlns='http://www.w3.org/2000/svg' width='400' height='150'>");
    buffer.write("<rect width='400' height='150' fill='white'/>");
    buffer.write("<path d='");

    bool penDown = false;
    for (int i = 0; i < _signaturePoints.length; i++) {
      final point = _signaturePoints[i];
      if (point == null) {
        penDown = false;
        continue;
      }
      if (!penDown) {
        buffer.write('M ${point.dx.toStringAsFixed(1)} ${point.dy.toStringAsFixed(1)} ');
        penDown = true;
      } else {
        buffer.write('L ${point.dx.toStringAsFixed(1)} ${point.dy.toStringAsFixed(1)} ');
      }
    }

    buffer.write("' stroke='#1e40af' stroke-width='2.5' fill='none' stroke-linecap='round'/>");
    buffer.write("</svg>");

    return 'data:image/svg+xml;charset=utf-8,${Uri.encodeComponent(buffer.toString())}';
  }

  Future<void> _handleSubmit() async {
    if (_hasSubmitted) return;

    if (!_formKey.currentState!.validate()) return;

    if (!_signatureCaptured) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Please provide a customer signature before submitting.'),
          backgroundColor: AppColors.critical,
        ),
      );
      return;
    }

    setState(() {
      _isSubmitting = true;
    });

    final signatureDataUrl = _buildSignatureDataUrl();

    final success = await context.read<WorkOrderProvider>().completeJob(
          widget.workOrderId,
          signerName: _customerNameController.text.trim(),
          signatureDataUrl: signatureDataUrl,
          notes: 'Job completed with customer sign-off captured on mobile.',
          photoFileKey: widget.uploadedFileKey,
        );

    if (mounted) {
      setState(() {
        _isSubmitting = false;
        if (success) _hasSubmitted = true;
      });

      if (success) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('✅ Work order completed and signed off successfully!'),
            backgroundColor: AppColors.completed,
            duration: Duration(seconds: 3),
          ),
        );
        // Pop back to root schedule
        Navigator.of(context).popUntil((route) => route.isFirst);
      } else {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(
            content: Text(
              context.read<WorkOrderProvider>().error ?? 'Failed to complete work order. Please try again.',
            ),
            backgroundColor: AppColors.critical,
          ),
        );
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final provider = context.watch<WorkOrderProvider>();
    final job = provider.currentJob;
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Scaffold(
      appBar: AppBar(
        title: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Digital Signature', style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold)),
            if (job != null)
              Text(
                job.workOrderNumber,
                style: TextStyle(
                  fontSize: 11,
                  color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                ),
              ),
          ],
        ),
      ),
      body: Form(
        key: _formKey,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            // Job Summary Card
            if (job != null)
              Card(
                shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Row(
                        children: const [
                          Icon(Icons.assignment_turned_in_outlined, color: AppColors.primary, size: 18),
                          SizedBox(width: 8),
                          Text(
                            'Work Order Summary',
                            style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                          ),
                        ],
                      ),
                      const Divider(height: 18),
                      _summaryRow('Work Order', job.workOrderNumber, isDark: isDark),
                      const SizedBox(height: 8),
                      _summaryRow('Issue', job.title, isDark: isDark),
                      const SizedBox(height: 8),
                      _summaryRow('Location', job.formattedLocation, isDark: isDark),
                    ],
                  ),
                ),
              ),
            const SizedBox(height: 14),

            // Execution Summary Card
            Card(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: const [
                        Icon(Icons.summarize_outlined, color: AppColors.primary, size: 18),
                        SizedBox(width: 8),
                        Text(
                          'Execution Summary',
                          style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                    const Divider(height: 18),
                    Row(
                      children: [
                        _statItem(Icons.timer_outlined, 'Time Spent', _formatDuration(widget.elapsedSeconds), isDark: isDark),
                        const Spacer(),
                        _statItem(Icons.speaker_notes_outlined, 'Field Notes', '${widget.noteCount}', isDark: isDark),
                        const Spacer(),
                        _statItem(Icons.photo_library_outlined, 'Photos', '${widget.photoCount}', isDark: isDark),
                      ],
                    ),
                    const SizedBox(height: 10),
                    Text(
                      'Completed: ${DateFormat('d MMM yyyy, hh:mm a').format(DateTime.now())}',
                      style: TextStyle(
                        fontSize: 12,
                        color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                      ),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 14),

            // Customer Name Input
            Card(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: const [
                        Icon(Icons.person_outline, color: AppColors.primary, size: 18),
                        SizedBox(width: 8),
                        Text(
                          'Customer Verification',
                          style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                        ),
                      ],
                    ),
                    const Divider(height: 18),
                    TextFormField(
                      controller: _customerNameController,
                      decoration: const InputDecoration(
                        labelText: 'Customer / Resident Full Name',
                        hintText: 'Enter the name of the person signing off',
                        prefixIcon: Icon(Icons.badge_outlined),
                      ),
                      validator: (value) {
                        if (value == null || value.trim().isEmpty) {
                          return 'Please enter the customer or resident full name';
                        }
                        if (value.trim().length < 2) {
                          return 'Name must be at least 2 characters';
                        }
                        return null;
                      },
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 14),

            // Signature Pad Card
            Card(
              shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      mainAxisAlignment: MainAxisAlignment.spaceBetween,
                      children: [
                        Row(
                          children: const [
                            Icon(Icons.draw_outlined, color: AppColors.primary, size: 18),
                            SizedBox(width: 8),
                            Text(
                              'Customer Signature',
                              style: TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                            ),
                          ],
                        ),
                        TextButton.icon(
                          onPressed: () {
                            setState(() {
                              _signaturePoints.clear();
                              _signatureCaptured = false;
                            });
                          },
                          icon: const Icon(Icons.clear, size: 14),
                          label: const Text('Clear', style: TextStyle(fontSize: 12)),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Text(
                      'Customer must sign below to confirm the completed work.',
                      style: TextStyle(
                        fontSize: 12,
                        color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                      ),
                    ),
                    const SizedBox(height: 12),

                    // Signature Canvas
                    Container(
                      height: 160,
                      width: double.infinity,
                      decoration: BoxDecoration(
                        color: isDark ? const Color(0xFF0F172A) : Colors.white,
                        borderRadius: BorderRadius.circular(10),
                        border: Border.all(
                          color: _signatureCaptured
                              ? AppColors.completed
                              : isDark
                                  ? AppColors.darkBorder
                                  : const Color(0xFFCBD5E1),
                          width: _signatureCaptured ? 2 : 1.5,
                        ),
                      ),
                      child: ClipRRect(
                        borderRadius: BorderRadius.circular(10),
                        child: GestureDetector(
                          onPanUpdate: (details) {
                            setState(() {
                              _signaturePoints.add(details.localPosition);
                              _signatureCaptured = true;
                            });
                          },
                          onPanEnd: (_) {
                            setState(() {
                              _signaturePoints.add(null);
                            });
                          },
                          child: Stack(
                            children: [
                              if (!_signatureCaptured)
                                Center(
                                  child: Text(
                                    'Sign here ↑',
                                    style: TextStyle(
                                      fontSize: 13,
                                      color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                                    ),
                                  ),
                                ),
                              CustomPaint(
                                painter: _SignaturePainter(
                                  points: _signaturePoints,
                                  isDark: isDark,
                                ),
                                size: Size.infinite,
                              ),
                            ],
                          ),
                        ),
                      ),
                    ),

                    if (_signatureCaptured) ...[
                      const SizedBox(height: 8),
                      Row(
                        children: const [
                          Icon(Icons.check_circle, color: AppColors.completed, size: 16),
                          SizedBox(width: 6),
                          Text(
                            'Signature captured',
                            style: TextStyle(
                              fontSize: 12,
                              fontWeight: FontWeight.w600,
                              color: AppColors.completed,
                            ),
                          ),
                        ],
                      ),
                    ],
                  ],
                ),
              ),
            ),
            const SizedBox(height: 24),

            // Submit & Complete Button
            SizedBox(
              height: 52,
              child: ElevatedButton.icon(
                onPressed: (_isSubmitting || _hasSubmitted) ? null : _handleSubmit,
                icon: _isSubmitting
                    ? const SizedBox(
                        width: 20,
                        height: 20,
                        child: CircularProgressIndicator(color: Colors.white, strokeWidth: 2.5),
                      )
                    : const Icon(Icons.verified_outlined, size: 22),
                label: Text(
                  _isSubmitting
                      ? 'Submitting to FixFlow...'
                      : _hasSubmitted
                          ? 'Submitted Successfully'
                          : 'Submit & Complete Work Order',
                  style: const TextStyle(fontSize: 15, fontWeight: FontWeight.bold),
                ),
                style: ElevatedButton.styleFrom(
                  backgroundColor: _hasSubmitted ? AppColors.completed : AppColors.primary,
                  foregroundColor: Colors.white,
                  shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(10)),
                ),
              ),
            ),
            const SizedBox(height: 8),
            Center(
              child: Text(
                'Submits signature and completion data to FixFlow ASP.NET Core backend.',
                style: TextStyle(
                  fontSize: 11,
                  color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
                ),
                textAlign: TextAlign.center,
              ),
            ),
            const SizedBox(height: 24),
          ],
        ),
      ),
    );
  }

  Widget _summaryRow(String label, String value, {required bool isDark}) {
    return Row(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        SizedBox(
          width: 80,
          child: Text(
            label,
            style: TextStyle(
              fontSize: 12,
              fontWeight: FontWeight.w600,
              color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
            ),
          ),
        ),
        const SizedBox(width: 8),
        Expanded(
          child: Text(
            value,
            style: TextStyle(
              fontSize: 13,
              fontWeight: FontWeight.w600,
              color: isDark ? AppColors.darkTextPrimary : AppColors.lightTextPrimary,
            ),
          ),
        ),
      ],
    );
  }

  Widget _statItem(IconData icon, String label, String value, {required bool isDark}) {
    return Column(
      children: [
        Icon(icon, color: AppColors.primary, size: 22),
        const SizedBox(height: 4),
        Text(
          value,
          style: TextStyle(
            fontSize: 18,
            fontWeight: FontWeight.bold,
            color: isDark ? AppColors.darkTextPrimary : AppColors.lightTextPrimary,
          ),
        ),
        Text(
          label,
          style: TextStyle(
            fontSize: 11,
            color: isDark ? AppColors.darkTextMuted : AppColors.lightTextMuted,
          ),
        ),
      ],
    );
  }
}

class _SignaturePainter extends CustomPainter {
  final List<Offset?> points;
  final bool isDark;

  _SignaturePainter({required this.points, required this.isDark});

  @override
  void paint(Canvas canvas, Size size) {
    final paint = Paint()
      ..color = isDark ? const Color(0xFF93C5FD) : const Color(0xFF1E40AF)
      ..strokeCap = StrokeCap.round
      ..strokeJoin = StrokeJoin.round
      ..strokeWidth = 2.8
      ..style = PaintingStyle.stroke;

    for (int i = 0; i < points.length - 1; i++) {
      if (points[i] != null && points[i + 1] != null) {
        canvas.drawLine(points[i]!, points[i + 1]!, paint);
      }
    }
  }

  @override
  bool shouldRepaint(covariant _SignaturePainter oldDelegate) =>
      oldDelegate.points != points;
}
