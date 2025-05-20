using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BlazorPanzoom.Events;
using BlazorPanzoom.Options;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace BlazorPanzoom.Services.Implementations;

public class PanzoomHelper(IJSBlazorPanzoomInterop jsPanzoomInterop) : IPanzoomHelper
{
	public async ValueTask SetTransformAsync(PanzoomInterop panzoom, EventCallback<SetTransformEventArgs> onSetTransform)
	{
		DotNetObjectReference<IPanzoom> dotNetRef = DotNetObjectReference.Create<IPanzoom>(panzoom);
		await jsPanzoomInterop.RegisterSetTransformAsync(dotNetRef, panzoom.JSPanzoomReference);
		panzoom.OnSetTransform += args => onSetTransform.InvokeAsync(args);
		panzoom.OnDispose += dotNetRef.Dispose;
	}

	public async ValueTask RegisterZoomWithWheelAsync(PanzoomInterop panzoom, ElementReference? elementReference = null)
	{
		await jsPanzoomInterop.RegisterZoomWithWheelAsync(panzoom.JSPanzoomReference, elementReference);

		async void RemoveTask()
		{
			await jsPanzoomInterop.RemoveZoomWithWheelAsync(panzoom.JSPanzoomReference, elementReference);
		}

		panzoom.OnDispose += RemoveTask;
	}

	public async ValueTask RegisterWheelListenerAsync(PanzoomInterop panzoom, EventCallback<CustomWheelEventArgs> onWheel, ElementReference? elementReference = null)
	{
		DotNetObjectReference<PanzoomInterop> dotNetRef = DotNetObjectReference.Create(panzoom);
		await jsPanzoomInterop.RegisterWheelListenerAsync(dotNetRef, panzoom.JSPanzoomReference, elementReference);

		async void RemoveTask()
		{
			await jsPanzoomInterop.RemoveWheelListenerAsync(panzoom.JSPanzoomReference, elementReference);
			dotNetRef.Dispose();
		}

		panzoom.OnCustomWheel += args => onWheel.InvokeAsync(args);
		panzoom.OnDispose += RemoveTask;
	}

	public async ValueTask RegisterWheelListenerAsync(PanzoomInterop panzoom, object receiver, Func<CustomWheelEventArgs, Task> onWheel, ElementReference? elementReference = null)
	{
		await RegisterWheelListenerAsync(panzoom, EventCallback.Factory.Create(receiver, onWheel),
			elementReference);
	}

	public async ValueTask<PanzoomInterop> CreateForElementReferenceAsync(ElementReference elementReference, PanzoomOptions? panzoomOptions = default)
	{
		IJSObjectReference panzoomRef = await jsPanzoomInterop.CreatePanzoomAsync(elementReference, panzoomOptions);
		PanzoomInterop panzoom = Create(panzoomRef);

		return panzoom;
	}

	public async ValueTask<PanzoomInterop[]> CreateForSelectorAsync(string selector, PanzoomOptions? panzoomOptions = default)
	{
		IJSObjectReference[] jsPanzoomReferences = await jsPanzoomInterop.CreatePanzoomAsync(selector, panzoomOptions);
		int referencesLength = jsPanzoomReferences.Length;
		PanzoomInterop[] panzoomControls = new PanzoomInterop[referencesLength];

		for (int i = 0; i < referencesLength; i++)
		{
			IJSObjectReference jsPanzoomRef = jsPanzoomReferences[i];
			panzoomControls[i] = Create(jsPanzoomRef);
		}

		return panzoomControls;
	}

	public async ValueTask ResetAllForAsync(IEnumerable<PanzoomInterop> panzoomInterops)
	{
		IJSObjectReference[] references = panzoomInterops.Select(p => p.JSPanzoomReference).ToArray();
		await jsPanzoomInterop.PerformForAllAsync("reset", references);
	}

	private PanzoomInterop Create(IJSObjectReference jsPanzoomReference)
	{
		async void DisposeTask()
		{
			await jsPanzoomInterop.DestroyPanzoomAsync(jsPanzoomReference);
		}

		PanzoomInterop panzoom = new PanzoomInterop(jsPanzoomReference);
		panzoom.OnDispose += DisposeTask;
		return panzoom;
	}
}