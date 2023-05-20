using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class SingleShotGun : Gun
{
	public struct ShooterInfo
	{
		public string shooterNickName;
	}

	public ShooterInfo shooter;

	[SerializeField] Camera cam;

	PhotonView PV;
	AudioSource asSound;
	public AudioClip shotSfx;

    void Awake()
	{
		PV = GetComponent<PhotonView>();
		asSound = GetComponent<AudioSource>();
	}

	public override void Use()
	{
		print("shot");
		Shoot();
	}

	void Shoot()
	{
		Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f));
		ray.origin = cam.transform.position;

		Collider[] colls = Physics.OverlapSphere(ray.origin, 1.0f, LayerMask.NameToLayer("Player"));

		if (colls.Length > 0)
		{
			shooter.shooterNickName = colls[0].GetComponent<PlayerController>().photonView.Owner.NickName;
		}

		if(Physics.Raycast(ray, out RaycastHit hit))
		{
			hit.collider.gameObject.GetComponent<IDamageable>()?.TakeDamage(((GunInfo)itemInfo).damage, shooter.shooterNickName);
			PV.RPC("RPC_Shoot", RpcTarget.All, hit.point, hit.normal);
		}
	}
	public Transform firPos;
	[PunRPC]
	void RPC_Shoot(Vector3 hitPosition, Vector3 hitNormal)
	{
		Collider[] colliders = Physics.OverlapSphere(hitPosition, 0.3f);
		if(colliders.Length != 0)
		{
			GameObject bulletImpactObj = Instantiate(bulletImpactPrefab, hitPosition + hitNormal * 0.001f, Quaternion.LookRotation(hitNormal, Vector3.up) * bulletImpactPrefab.transform.rotation);
			Destroy(bulletImpactObj, 10f);
			bulletImpactObj.transform.SetParent(colliders[0].transform);
            GameObject bulletHoleObj = Instantiate(bulletHolePrefab, hitPosition + hitNormal * 0.001f, Quaternion.LookRotation(hitNormal, Vector3.up) * bulletHolePrefab.transform.rotation);
            Destroy(bulletHoleObj, 10f);
            bulletHoleObj.transform.SetParent(colliders[0].transform);
			asSound.PlayOneShot(shotSfx);
        }
    }
}
