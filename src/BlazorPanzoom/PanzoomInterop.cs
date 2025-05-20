using System;
using System.Threading.Tasks;
using BlazorPanzoom.Events;
using BlazorPanzoom.Extensions;
using BlazorPanzoom.Options;
using Microsoft.JSInterop;

namespace BlazorPanzoom;

public class PanzoomInterop : IPanzoom, IAsyncDisposable
{
	public IJSObjectReference JSPanzoomReference { get; }

	public event BlazorPanzoomEventHandler<CustomWheelEventArgs>? OnCustomWheel;
	public event BlazorPanzoomEventHandler<SetTransformEventArgs>? OnSetTransform;
	public event BlazorPanzoomEventHandler? OnDispose;

	private bool _isDisposed;

	internal PanzoomInterop(IJSObjectReference jsPanzoomReference)
	{
		JSPanzoomReference = jsPanzoomReference;
	}

	public async ValueTask DisposeAsync()
	{
		if (_isDisposed)
		{
			return;
		}

		_isDisposed = true;

		GC.SuppressFinalize(this);
		
		// 1. Invoke C# event handler (less likely to fail with JSObjectReference error, but good practice to protect)
		try
		{
			if (OnDispose != null)
			{
				await OnDispose.InvokeAsync();
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Error invoking OnDispose event: {ex.Message}");
		}

		// 2. Attempt to destroy the JS-side panzoom instance
		await DestroyAsync();

		// 3. Attempt to dispose the JSObjectReference itself
		try
		{
			await JSPanzoomReference.DisposeAsync();
		}
		catch (ObjectDisposedException)
		{
			Console.WriteLine("PanzoomInterop: _jsPanzoomReference was already disposed when DisposeAsync tried to dispose it. Ignoring.");
		}
		catch (JSDisconnectedException)
		{
			Console.WriteLine("PanzoomInterop: JS disconnected when DisposeAsync tried to dispose _jsPanzoomReference. Ignoring.");
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
		{
			Console.WriteLine($"PanzoomInterop: JS interop unavailable during _jsPanzoomReference.DisposeAsync: {ex.Message}. Ignoring.");
		}
		catch (Exception ex)
		{
			Console.WriteLine($"PanzoomInterop: Unexpected error disposing _jsPanzoomReference: {ex.Message}");
		}

		// 4. Clean up C# event handlers (safe operation)
		DisposeAllEventHandlers();
	}

	public async ValueTask PanAsync(double x, double y, IPanOnlyOptions? overridenOptions = default)
	{
		await JSPanzoomReference.InvokeVoidAsync("pan", x, y, overridenOptions);
	}

	public async ValueTask ZoomInAsync(IZoomOnlyOptions? options = default)
	{
		await JSPanzoomReference.InvokeVoidAsync("zoomIn");
	}

	public async ValueTask ZoomOutAsync(IZoomOnlyOptions? options = default)
	{
		await JSPanzoomReference.InvokeVoidAsync("zoomOut");
	}

	public async ValueTask ZoomAsync(double toScale, IZoomOnlyOptions? options = default)
	{
		await JSPanzoomReference.InvokeVoidAsync("zoom", toScale);
	}

	public async ValueTask ZoomToPointAsync(double toScale, double clientX, double clientY, IZoomOnlyOptions? overridenZoomOptions = default)
	{
		await JSPanzoomReference.InvokeVoidAsync("zoomToPoint", toScale, new PointArgs(clientX, clientY),
			overridenZoomOptions);
	}

	public async ValueTask ZoomWithWheelAsync(CustomWheelEventArgs args, IZoomOnlyOptions? overridenOptions = default)
	{
		PanzoomOptions currentOptions = await GetOptionsAsync();
		double currentScale = await GetScaleAsync();
		double minScale = currentOptions.GetMinScaleOrDefault();
		double maxScale = currentOptions.GetMaxScaleOrDefault();
		double step = currentOptions.GetStepOrDefault();
		if (overridenOptions is not null)
		{
			minScale = overridenOptions.GetMinScaleOrDefault(minScale);
			maxScale = overridenOptions.GetMaxScaleOrDefault(maxScale);
			step = overridenOptions.GetStepOrDefault(step);
		}

		double delta = args.DeltaY == 0 && args.DeltaX != 0 ? args.DeltaX : args.DeltaY;
		int direction = delta < 0 ? 1 : -1;
		double calculatedScale = currentScale * Math.Exp(direction * step / 3);
		double constrainedScale = Math.Min(Math.Max(calculatedScale, minScale), maxScale);
		await ZoomToPointAsync(constrainedScale, args.ClientX, args.ClientY, overridenOptions);
	}

	public async ValueTask ResetAsync(PanzoomOptions? options = default)
	{
		await JSPanzoomReference.InvokeVoidAsync("reset");
	}

	public async ValueTask SetOptionsAsync(PanzoomOptions options)
	{
		// TODO not allowed to set Force option
		await JSPanzoomReference.InvokeVoidAsync("setOptions", options);
	}

	public async ValueTask<PanzoomOptions> GetOptionsAsync()
	{
		return await JSPanzoomReference.InvokeAsync<PanzoomOptions>("getOptions");
	}

	public async ValueTask<double> GetScaleAsync()
	{
		return await JSPanzoomReference.InvokeAsync<double>("getScale");
	}

	public async ValueTask<ReadOnlyFocalPoint> GetPanAsync()
	{
		return await JSPanzoomReference.InvokeAsync<ReadOnlyFocalPoint>("getPan");
	}

	public async ValueTask ResetStyleAsync()
	{
		await JSPanzoomReference.InvokeVoidAsync("resetStyle");
	}

	public async ValueTask SetStyleAsync(string name, string value)
	{
		await JSPanzoomReference.InvokeVoidAsync("setStyle", name, value);
	}

	public async ValueTask DestroyAsync()
	{
		if (_isDisposed)
		{
			return;
		}

		try
		{
			await JSPanzoomReference.InvokeVoidAsync("destroy");
		}
		catch (ObjectDisposedException)
		{
			// The JSObjectReference was already disposed. This is okay during cleanup.
			_isDisposed = true;
		}
		catch (JSDisconnectedException)
		{
			// The JS runtime is unavailable (e.g., SignalR connection lost).
			_isDisposed = true;
		}
		catch (InvalidOperationException ex) when (ex.Message.Contains("JavaScript interop calls cannot be issued"))
		{
			// Another symptom of disconnection or prerendering disposal issues.
			_isDisposed = true;
		}
		catch (Exception ex)
		{
			// Catch unexpected errors during the destroy call
			_isDisposed = true;
		}
	}

	[JSInvokable]
	public async ValueTask OnCustomWheelEvent(CustomWheelEventArgs args)
	{
		await OnCustomWheel.InvokeAsync(args);
	}

	[JSInvokable]
	public async ValueTask OnSetTransformEvent(SetTransformEventArgs args)
	{
		await OnSetTransform.InvokeAsync(args);
	}

	private void DisposeAllEventHandlers()
	{
		OnCustomWheel = null;
		OnSetTransform = null;
		OnDispose = null;
	}

	protected bool Equals(PanzoomInterop other)
	{
		return JSPanzoomReference.Equals(other.JSPanzoomReference);
	}

	public override bool Equals(object? obj)
	{
		if (ReferenceEquals(null, obj)) return false;
		if (ReferenceEquals(this, obj)) return true;
		if (obj.GetType() != GetType()) return false;
		return Equals((PanzoomInterop)obj);
	}

	public override int GetHashCode()
	{
		return JSPanzoomReference.GetHashCode();
	}

	public static bool operator ==(PanzoomInterop? left, PanzoomInterop? right)
	{
		return Equals(left, right);
	}

	public static bool operator !=(PanzoomInterop? left, PanzoomInterop? right)
	{
		return !Equals(left, right);
	}
}